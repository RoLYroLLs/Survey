using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Persistence;
using Survey.Infrastructure.Security;

namespace Survey.Infrastructure.Services;

public sealed class TenantContextAccessor(
	AuthenticationStateProvider authenticationStateProvider,
	IHttpContextAccessor httpContextAccessor,
	TenantExecutionContext tenantExecutionContext,
	IServiceScopeFactory serviceScopeFactory) : ITenantContextAccessor
{
	private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider;
	private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
	private readonly TenantExecutionContext _tenantExecutionContext = tenantExecutionContext;
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

	public async Task<CurrentAccessContext> GetCurrentAsync(CancellationToken cancellationToken = default)
	{
		var principal = await ResolvePrincipalAsync();
		if (principal.Identity?.IsAuthenticated != true)
		{
			_tenantExecutionContext.Clear();
			return new CurrentAccessContext();
		}

		using var scope = _serviceScopeFactory.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var dbContext = scope.ServiceProvider.GetRequiredService<SurveyDbContext>();

		var user = await userManager.GetUserAsync(principal);
		if (user is null)
		{
			_tenantExecutionContext.Clear();
			return new CurrentAccessContext();
		}

		var memberships = await dbContext.TenantMemberships
			.AsNoTracking()
			.Include(membership => membership.Tenant)
			.Include(membership => membership.PermissionOverrides)
			.Where(membership => membership.UserId == user.Id)
			.OrderBy(membership => membership.Id)
			.ToListAsync(cancellationToken);

		var activeMembership = memberships.FirstOrDefault(membership => membership.Id == user.ActiveTenantMembershipId && membership.IsEnabled);
		if (activeMembership is null)
		{
			var enabledMemberships = memberships.Where(membership => membership.IsEnabled).ToList();
			if (enabledMemberships.Count == 1)
			{
				activeMembership = enabledMemberships[0];
				await UpdateActiveMembershipAsync(userManager, user, activeMembership.Id);
			}
			else if (user.ActiveTenantMembershipId.HasValue)
			{
				await UpdateActiveMembershipAsync(userManager, user, null);
			}
		}

		var platformPermissions = await ResolvePlatformPermissionsAsync(dbContext, user, cancellationToken);
		if (activeMembership is null)
		{
			_tenantExecutionContext.Clear();
			return new CurrentAccessContext
			{
				UserId = user.Id,
				Email = user.Email,
				IsAuthenticated = true,
				IsPlatformSuperAdmin = user.IsPlatformSuperAdmin,
				IsPlatformUserEnabled = user.IsPlatformUserEnabled,
				PlatformPermissions = platformPermissions
			};
		}

		var tenantPermissions = ResolveTenantPermissions(activeMembership);
		_tenantExecutionContext.UseTenant(activeMembership.TenantId);

		return new CurrentAccessContext
		{
			UserId = user.Id,
			Email = user.Email,
			IsAuthenticated = true,
			IsPlatformSuperAdmin = user.IsPlatformSuperAdmin,
			IsPlatformUserEnabled = user.IsPlatformUserEnabled,
			ActiveTenantMembershipId = activeMembership.Id,
			TenantId = activeMembership.TenantId,
			TenantName = activeMembership.Tenant.Name,
			TenantRole = activeMembership.Role,
			TenantMembershipEnabled = activeMembership.IsEnabled,
			TenantPermissions = tenantPermissions,
			PlatformPermissions = platformPermissions
		};
	}

	public async Task<IReadOnlyList<TenantMembershipOption>> GetMembershipOptionsAsync(CancellationToken cancellationToken = default)
	{
		var principal = await ResolvePrincipalAsync();
		if (principal.Identity?.IsAuthenticated != true)
		{
			return [];
		}

		using var scope = _serviceScopeFactory.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var dbContext = scope.ServiceProvider.GetRequiredService<SurveyDbContext>();

		var user = await userManager.GetUserAsync(principal);
		if (user is null)
		{
			return [];
		}

		var memberships = await dbContext.TenantMemberships
			.AsNoTracking()
			.Include(membership => membership.Tenant)
			.Where(membership => membership.UserId == user.Id)
			.OrderBy(membership => membership.Tenant.Name)
			.ToListAsync(cancellationToken);
		if (memberships.Count == 0)
		{
			return [];
		}

		var tenantIds = memberships
			.Select(membership => membership.TenantId)
			.Distinct()
			.ToArray();
		var ownerMemberships = await dbContext.TenantMemberships
			.AsNoTracking()
			.Where(membership => tenantIds.Contains(membership.TenantId) && membership.Role == TenantRole.Owner)
			.Select(membership => new
			{
				membership.TenantId,
				membership.UserId
			})
			.ToListAsync(cancellationToken);
		var ownerUserIds = ownerMemberships
			.Select(membership => membership.UserId)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		var ownerLookup = await userManager.Users
			.AsNoTracking()
			.Where(item => ownerUserIds.Contains(item.Id))
			.ToDictionaryAsync(
				item => item.Id,
				item => new
				{
					DisplayName = string.IsNullOrWhiteSpace(item.FirstName) && string.IsNullOrWhiteSpace(item.LastName)
						? (item.Email ?? item.UserName ?? "Unknown user")
						: string.Join(" ", new[] { item.FirstName, item.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))),
					OrganizationName = item.IsOrganizationAccount ? item.OrganizationName ?? string.Empty : string.Empty
				},
				cancellationToken);
		var tenantOwnerLookup = ownerMemberships
			.GroupBy(membership => membership.TenantId)
			.ToDictionary(
				group => group.Key,
				group =>
				{
					var ownerMembership = group.First();
					return ownerLookup.TryGetValue(ownerMembership.UserId, out var owner)
						? owner
						: new
						{
							DisplayName = string.Empty,
							OrganizationName = string.Empty
						};
				});

		return memberships
			.Select(membership => new TenantMembershipOption
			{
				MembershipId = membership.Id,
				TenantId = membership.TenantId,
				TenantName = membership.Tenant.Name,
				OwnerDisplayName = tenantOwnerLookup.GetValueOrDefault(membership.TenantId)?.DisplayName ?? string.Empty,
				DropdownSubtitle = !string.IsNullOrWhiteSpace(tenantOwnerLookup.GetValueOrDefault(membership.TenantId)?.OrganizationName)
					? tenantOwnerLookup.GetValueOrDefault(membership.TenantId)!.OrganizationName
					: tenantOwnerLookup.GetValueOrDefault(membership.TenantId)?.DisplayName ?? string.Empty,
				Role = membership.Role,
				IsEnabled = membership.IsEnabled,
				IsActive = membership.Id == user.ActiveTenantMembershipId
			})
			.ToList();
	}

	public async Task<CurrentAccessContext> RequireTenantContextAsync(CancellationToken cancellationToken = default)
	{
		var context = await GetCurrentAsync(cancellationToken);
		if (!context.IsAuthenticated)
		{
			throw new UnauthorizedAccessException("Authentication is required.");
		}

		if (!context.HasTenantAccess || !context.TenantId.HasValue)
		{
			throw new UnauthorizedAccessException("An active tenant is required.");
		}

		_tenantExecutionContext.UseTenant(context.TenantId.Value);
		return context;
	}

	public async Task<CurrentAccessContext> RequirePlatformContextAsync(CancellationToken cancellationToken = default)
	{
		var context = await GetCurrentAsync(cancellationToken);
		if (!context.IsAuthenticated || !context.IsPlatformUserEnabled || context.PlatformPermissions.Count == 0)
		{
			throw new UnauthorizedAccessException("Platform access is required.");
		}

		_tenantExecutionContext.UsePlatformBypass();
		return context;
	}

	public async Task SwitchActiveTenantAsync(int membershipId, CancellationToken cancellationToken = default)
	{
		var principal = await ResolvePrincipalAsync();
		if (principal.Identity?.IsAuthenticated != true)
		{
			throw new UnauthorizedAccessException("Authentication is required.");
		}

		using var scope = _serviceScopeFactory.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var dbContext = scope.ServiceProvider.GetRequiredService<SurveyDbContext>();

		var user = await userManager.GetUserAsync(principal)
			?? throw new UnauthorizedAccessException("The current user was not found.");
		var membership = await dbContext.TenantMemberships
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Id == membershipId && item.UserId == user.Id && item.IsEnabled, cancellationToken)
			?? throw new UnauthorizedAccessException("The requested tenant membership was not found.");

		await UpdateActiveMembershipAsync(userManager, user, membership.Id);
		_tenantExecutionContext.UseTenant(membership.TenantId);
	}

	private async Task<System.Security.Claims.ClaimsPrincipal> ResolvePrincipalAsync()
	{
		var httpContextUser = _httpContextAccessor.HttpContext?.User;
		if (httpContextUser is not null
			&& (httpContextUser.Identity?.IsAuthenticated == true || httpContextUser.Claims.Any()))
		{
			return httpContextUser;
		}

		try
		{
			var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
			return authenticationState.User;
		}
		catch (InvalidOperationException)
		{
			return new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
		}
	}

	private static async Task UpdateActiveMembershipAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, int? membershipId)
	{
		if (user.ActiveTenantMembershipId == membershipId)
		{
			return;
		}

		user.ActiveTenantMembershipId = membershipId;
		await userManager.UpdateAsync(user);
	}

	private static async Task<IReadOnlySet<string>> ResolvePlatformPermissionsAsync(SurveyDbContext dbContext, ApplicationUser user, CancellationToken cancellationToken)
	{
		if (!user.IsPlatformUserEnabled)
		{
			return new HashSet<string>(StringComparer.Ordinal);
		}

		if (user.IsPlatformSuperAdmin)
		{
			return PermissionDefaults.GetPlatformAdminPermissions();
		}

		return await dbContext.PlatformUserPermissions
			.AsNoTracking()
			.Where(permission => permission.UserId == user.Id)
			.Select(permission => permission.PermissionKey)
			.ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
	}

	private static IReadOnlySet<string> ResolveTenantPermissions(TenantMembership membership)
	{
		var permissions = PermissionDefaults.GetTenantPermissions(membership.Role).ToHashSet(StringComparer.Ordinal);
		foreach (var overridePermission in membership.PermissionOverrides)
		{
			if (overridePermission.GrantKind == PermissionGrantKind.Allow)
			{
				permissions.Add(overridePermission.PermissionKey);
			}
			else
			{
				permissions.Remove(overridePermission.PermissionKey);
			}
		}

		return permissions;
	}
}
