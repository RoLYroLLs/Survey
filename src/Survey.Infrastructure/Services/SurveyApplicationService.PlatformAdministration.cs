using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<PagedResult<PlatformUserListItem>> GetPlatformUsersAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersView, cancellationToken);

		var users = await _userManager.Users
			.AsNoTracking()
			.Include(user => user.PlatformPermissions)
			.OrderBy(user => user.LastName)
			.ThenBy(user => user.FirstName)
			.ThenBy(user => user.Email)
			.ToListAsync(cancellationToken);

		var items = users
			.Where(user => string.IsNullOrWhiteSpace(search) || MatchesPlatformUserSearch(user, search!))
			.Select(user => new PlatformUserListItem
			{
				Id = user.Id,
				FirstName = user.FirstName ?? string.Empty,
				LastName = user.LastName ?? string.Empty,
				Email = user.Email ?? string.Empty,
				IsPlatformUserEnabled = user.IsPlatformUserEnabled,
				IsPlatformSuperAdmin = user.IsPlatformSuperAdmin,
				PermissionCount = user.IsPlatformSuperAdmin
					? PlatformPermissionCatalog.All.Count
					: user.PlatformPermissions.Count
			})
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["name"] = [nameof(PlatformUserListItem.LastName), nameof(PlatformUserListItem.FirstName)],
			["email"] = [nameof(PlatformUserListItem.Email)],
			["status"] = [nameof(PlatformUserListItem.IsPlatformUserEnabled)],
			["scope"] = [nameof(PlatformUserListItem.IsPlatformSuperAdmin)],
			["permissions"] = [nameof(PlatformUserListItem.PermissionCount)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(PlatformUserListItem.Id)).ToList();
		var normalizedRequest = NormalizePagedQuery(request);
		var totalCount = orderedItems.Count;
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, totalCount, normalizedRequest.Offset);
	}

	public async Task<PlatformUserEditModel> GetPlatformUserAsync(string? id, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersView, cancellationToken);

		var currentUserId = await RequireCurrentUserIdAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(id))
		{
			return BuildPlatformUserEditModel(null, currentUserId);
		}

		var user = await _userManager.Users
			.AsNoTracking()
			.Include(item => item.PlatformPermissions)
			.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested platform user was not found.");

		return BuildPlatformUserEditModel(user, currentUserId);
	}

	public async Task<string> SavePlatformUserAsync(PlatformUserEditModel model, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersManage, cancellationToken);

		var actor = await RequirePlatformAccessAsync(cancellationToken);
		var desiredPermissionKeys = model.Permissions
			.Where(permission => permission.Selected)
			.Select(permission => permission.PermissionKey)
			.Distinct(StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
		var requiresPermissionManagement = model.IsPlatformSuperAdmin || desiredPermissionKeys.Count > 0;
		if (requiresPermissionManagement)
		{
			await RequirePlatformPermissionAsync(PlatformPermissionKeys.PermissionsManage, cancellationToken);
		}

		ApplicationUser user;
		bool isNewUser;
		bool existingEnabled;
		bool existingSuperAdmin;
		HashSet<string> existingPermissions;

		if (string.IsNullOrWhiteSpace(model.Id))
		{
			isNewUser = true;
			existingEnabled = false;
			existingSuperAdmin = false;
			existingPermissions = new HashSet<string>(StringComparer.Ordinal);

			if (string.IsNullOrWhiteSpace(model.Password))
			{
				throw new InvalidOperationException("A password is required when creating a new platform user.");
			}

			user = new ApplicationUser
			{
				UserName = model.Email.Trim(),
				Email = model.Email.Trim(),
				EmailConfirmed = true,
				FirstName = model.FirstName.Trim(),
				LastName = model.LastName.Trim(),
				IsPlatformUserEnabled = model.IsPlatformUserEnabled,
				IsPlatformSuperAdmin = model.IsPlatformSuperAdmin
			};
			UserAvatarPalette.EnsureAssigned(user);

			var createResult = await _userManager.CreateAsync(user, model.Password);
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(static error => error.Description)));
			}
		}
		else
		{
			isNewUser = false;
			user = await _userManager.Users
				.Include(item => item.PlatformPermissions)
				.FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken)
				?? throw new InvalidOperationException("The requested platform user was not found.");

			existingEnabled = user.IsPlatformUserEnabled;
			existingSuperAdmin = user.IsPlatformSuperAdmin;
			existingPermissions = user.PlatformPermissions
				.Select(permission => permission.PermissionKey)
				.ToHashSet(StringComparer.Ordinal);

			var accessStateChanged = existingEnabled != model.IsPlatformUserEnabled
				|| existingSuperAdmin != model.IsPlatformSuperAdmin
				|| !existingPermissions.SetEquals(desiredPermissionKeys);
			if (string.Equals(user.Id, actor.UserId, StringComparison.Ordinal) && accessStateChanged)
			{
				throw new InvalidOperationException("You cannot change your own platform access, role, or permissions.");
			}

			user.UserName = model.Email.Trim();
			user.Email = model.Email.Trim();
			user.FirstName = model.FirstName.Trim();
			user.LastName = model.LastName.Trim();
			user.IsPlatformUserEnabled = model.IsPlatformUserEnabled;
			user.IsPlatformSuperAdmin = model.IsPlatformSuperAdmin;

			var updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(static error => error.Description)));
			}
		}

		if (!isNewUser)
		{
			if (!string.IsNullOrWhiteSpace(model.Password))
			{
				if (await _userManager.HasPasswordAsync(user))
				{
					var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
					var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
					if (!resetResult.Succeeded)
					{
						throw new InvalidOperationException(string.Join("; ", resetResult.Errors.Select(static error => error.Description)));
					}
				}
				else
				{
					var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
					if (!addPasswordResult.Succeeded)
					{
						throw new InvalidOperationException(string.Join("; ", addPasswordResult.Errors.Select(static error => error.Description)));
					}
				}
			}
		}

		await ApplyPlatformUserPermissionsAsync(user, model.IsPlatformSuperAdmin ? [] : desiredPermissionKeys, cancellationToken);
		await EnsurePlatformUserGuardrailsAsync(user.Id, model.IsPlatformUserEnabled, model.IsPlatformSuperAdmin, model.IsPlatformSuperAdmin ? [] : desiredPermissionKeys, cancellationToken);

		var saveResult = await _userManager.UpdateAsync(user);
		if (!saveResult.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", saveResult.Errors.Select(static error => error.Description)));
		}

		if (isNewUser)
		{
			await _auditWriter.WriteAsync("platform", "platform.user.created", nameof(ApplicationUser), user.Id, $"Created platform user '{user.Email}'.", true, cancellationToken);
		}
		else
		{
			await _auditWriter.WriteAsync("platform", "platform.user.updated", nameof(ApplicationUser), user.Id, $"Updated platform user '{user.Email}'.", true, cancellationToken);
		}

		return user.Id;
	}

	public async Task<PagedResult<PlatformTenantListItem>> GetPlatformTenantsAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.TenantsView, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var query = _dbContext.Tenants
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(tenant =>
				tenant.Name.Contains(term) ||
				tenant.Slug.Contains(term));
		}

		var tenants = await query
			.OrderBy(tenant => tenant.Name)
			.ThenBy(tenant => tenant.Id)
			.Select(tenant => new PlatformTenantListItem
			{
				Id = tenant.Id,
				Name = tenant.Name,
				Slug = tenant.Slug,
				MembershipCount = tenant.Memberships.Count,
				EnabledMembershipCount = tenant.Memberships.Count(membership => membership.IsEnabled),
				OwnerCount = tenant.Memberships.Count(membership => membership.IsEnabled && membership.Role == TenantRole.Owner),
				PendingInvitationCount = 0,
				CreatedUtc = tenant.CreatedUtc,
				UpdatedUtc = tenant.UpdatedUtc
			})
			.ToListAsync(cancellationToken);
		var normalizedRequest = NormalizePagedQuery(request);
		if (tenants.Count == 0)
		{
			return CreatePagedResult<PlatformTenantListItem>([], 0, normalizedRequest.Offset);
		}

		var tenantIds = tenants.Select(item => item.Id).ToHashSet();
		var nowUtc = DateTimeOffset.UtcNow;
		var pendingInvitationCounts = (await _dbContext.TenantInvitations
			.AsNoTracking()
			.Where(invitation =>
				invitation.AcceptedUtc == null &&
				invitation.RevokedUtc == null)
			.Select(invitation => new
			{
				invitation.TenantId,
				invitation.ExpiresAtUtc
			})
			.ToListAsync(cancellationToken))
			.Where(invitation => tenantIds.Contains(invitation.TenantId) && invitation.ExpiresAtUtc > nowUtc)
			.GroupBy(invitation => invitation.TenantId)
			.ToDictionary(group => group.Key, group => group.Count());

		foreach (var item in tenants)
		{
			item.PendingInvitationCount = pendingInvitationCounts.GetValueOrDefault(item.Id);
		}

		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["tenant"] = [nameof(PlatformTenantListItem.Name), nameof(PlatformTenantListItem.Slug)],
			["members"] = [nameof(PlatformTenantListItem.MembershipCount)],
			["enabled"] = [nameof(PlatformTenantListItem.EnabledMembershipCount)],
			["owners"] = [nameof(PlatformTenantListItem.OwnerCount)],
			["pendinginvites"] = [nameof(PlatformTenantListItem.PendingInvitationCount)],
			["updated"] = [nameof(PlatformTenantListItem.UpdatedUtc)]
		};
		var orderedItems = ApplyRequestedSorts(tenants.AsQueryable(), request, sortMap, nameof(PlatformTenantListItem.Id)).ToList();
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, orderedItems.Count, normalizedRequest.Offset);
	}

	public async Task<PlatformTenantDetailModel> GetPlatformTenantAsync(int tenantId, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.TenantsOversight, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var tenant = await _dbContext.Tenants
			.AsNoTracking()
			.Include(item => item.Memberships)
				.ThenInclude(membership => membership.PermissionOverrides)
			.Include(item => item.Invitations)
			.Include(item => item.Settings)
			.FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested tenant was not found.");

		var userIds = tenant.Memberships.Select(membership => membership.UserId).Distinct(StringComparer.Ordinal).ToArray();
		var users = await _userManager.Users
			.AsNoTracking()
			.Where(user => userIds.Contains(user.Id))
			.ToDictionaryAsync(user => user.Id, cancellationToken);

		return new PlatformTenantDetailModel
		{
			Id = tenant.Id,
			Name = tenant.Name,
			Slug = tenant.Slug,
			ThemePresetKey = tenant.Settings.FirstOrDefault()?.ThemePresetKey ?? SiteThemePresetCatalog.DefaultPresetKey,
			MembershipCount = tenant.Memberships.Count,
			EnabledMembershipCount = tenant.Memberships.Count(membership => membership.IsEnabled),
			OwnerCount = tenant.Memberships.Count(membership => membership.IsEnabled && membership.Role == TenantRole.Owner),
			PendingInvitationCount = tenant.Invitations.Count(invitation => invitation.AcceptedUtc == null && invitation.RevokedUtc == null && invitation.ExpiresAtUtc > DateTimeOffset.UtcNow),
			PeopleCount = await _dbContext.People.CountAsync(person => person.TenantId == tenantId, cancellationToken),
			LocationCount = await _dbContext.Locations.CountAsync(location => location.TenantId == tenantId, cancellationToken),
			SurveyCount = await _dbContext.SurveyDefinitions.CountAsync(definition => definition.TenantId == tenantId, cancellationToken),
			AssignmentCount = await _dbContext.SurveyAssignments.CountAsync(assignment => assignment.TenantId == tenantId, cancellationToken),
			ResponseCount = await _dbContext.SurveyResponses.CountAsync(response => response.TenantId == tenantId, cancellationToken),
			GoalCount = await _dbContext.Goals.CountAsync(goal => goal.TenantId == tenantId, cancellationToken),
			AreaCount = await _dbContext.Areas.CountAsync(area => area.TenantId == tenantId, cancellationToken),
			CreatedUtc = tenant.CreatedUtc,
			UpdatedUtc = tenant.UpdatedUtc,
			Memberships = tenant.Memberships
				.OrderBy(membership => membership.Role)
				.ThenBy(membership => users.TryGetValue(membership.UserId, out var user) ? user.LastName : null)
				.ThenBy(membership => users.TryGetValue(membership.UserId, out var user) ? user.FirstName : null)
				.Select(membership =>
				{
					users.TryGetValue(membership.UserId, out var user);
					return new PlatformTenantMembershipListItem
					{
						MembershipId = membership.Id,
						UserId = membership.UserId,
						FullName = BuildDisplayName(user?.FirstName, user?.LastName, user?.Email),
						Email = user?.Email ?? string.Empty,
						Role = membership.Role,
						IsEnabled = membership.IsEnabled,
						PermissionOverrideCount = membership.PermissionOverrides.Count,
						EffectivePermissionCount = ResolveEffectiveTenantPermissions(membership).Count
					};
				})
				.ToList()
		};
	}

	public async Task<PagedResult<AuditLogListItem>> GetAuditLogsAsync(
		PagedQuery request,
		string? plane = null,
		int? tenantId = null,
		bool? succeeded = null,
		string? search = null,
		CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.AuditView, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var query = _dbContext.AuditLogs
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(plane))
		{
			query = query.Where(log => log.Plane == plane);
		}

		if (tenantId.HasValue)
		{
			query = query.Where(log => log.TenantId == tenantId.Value);
		}

		if (succeeded.HasValue)
		{
			query = query.Where(log => log.Succeeded == succeeded.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(log =>
				log.ActionType.Contains(term) ||
				log.TargetType.Contains(term) ||
				(log.TargetId != null && log.TargetId.Contains(term)) ||
				(log.Details != null && log.Details.Contains(term)));
		}

		var normalizedRequest = NormalizePagedQuery(request);
		var logs = await query.ToListAsync(cancellationToken);
		var tenantIds = logs
			.Where(log => log.TenantId.HasValue)
			.Select(log => log.TenantId!.Value)
			.Distinct()
			.ToArray();
		var actorUserIds = logs
			.Where(log => !string.IsNullOrWhiteSpace(log.ActorUserId))
			.Select(log => log.ActorUserId!)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		var tenantLookup = await _dbContext.Tenants
			.AsNoTracking()
			.Where(tenant => tenantIds.Contains(tenant.Id))
			.ToDictionaryAsync(tenant => tenant.Id, tenant => tenant.Name, cancellationToken);
		var userLookup = await _userManager.Users
			.AsNoTracking()
			.Where(user => actorUserIds.Contains(user.Id))
			.ToDictionaryAsync(user => user.Id, user => BuildDisplayName(user.FirstName, user.LastName, user.Email), cancellationToken);

		var items = logs
			.OrderByDescending(log => log.CreatedUtc)
			.ThenByDescending(log => log.Id)
			.Select(log => new AuditLogListItem
			{
				Id = log.Id,
				TenantId = log.TenantId,
				TenantName = log.TenantId.HasValue ? tenantLookup.GetValueOrDefault(log.TenantId.Value) : null,
				ActorUserId = log.ActorUserId,
				ActorDisplayName = !string.IsNullOrWhiteSpace(log.ActorUserId)
					? userLookup.GetValueOrDefault(log.ActorUserId, log.ActorUserId)
					: "Anonymous / system",
				Plane = log.Plane,
				ActionType = log.ActionType,
				TargetType = log.TargetType,
				TargetId = log.TargetId,
				Details = log.Details,
				Succeeded = log.Succeeded,
				CreatedUtc = log.CreatedUtc
			})
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["when"] = [nameof(AuditLogListItem.CreatedUtc)],
			["plane"] = [nameof(AuditLogListItem.Plane)],
			["tenant"] = [nameof(AuditLogListItem.TenantName)],
			["actor"] = [nameof(AuditLogListItem.ActorDisplayName)],
			["action"] = [nameof(AuditLogListItem.ActionType)],
			["target"] = [nameof(AuditLogListItem.TargetType), nameof(AuditLogListItem.TargetId)],
			["result"] = [nameof(AuditLogListItem.Succeeded)],
			["details"] = [nameof(AuditLogListItem.Details)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(AuditLogListItem.Id), tieBreakerDescending: true).ToList();
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, orderedItems.Count, normalizedRequest.Offset);
	}

	private PlatformUserEditModel BuildPlatformUserEditModel(ApplicationUser? user, string currentUserId)
	{
		var selectedPermissions = user?.PlatformPermissions
			.Select(permission => permission.PermissionKey)
			.ToHashSet(StringComparer.Ordinal)
			?? new HashSet<string>(StringComparer.Ordinal);

		return new PlatformUserEditModel
		{
			Id = user?.Id,
			FirstName = user?.FirstName ?? string.Empty,
			LastName = user?.LastName ?? string.Empty,
			Email = user?.Email ?? string.Empty,
			IsPlatformUserEnabled = user?.IsPlatformUserEnabled ?? false,
			IsPlatformSuperAdmin = user?.IsPlatformSuperAdmin ?? false,
			IsCurrentUser = user is not null && string.Equals(user.Id, currentUserId, StringComparison.Ordinal),
			Permissions = PlatformPermissionCatalog.All
				.Select(definition => new PlatformUserPermissionEditModel
				{
					PermissionKey = definition.Key,
					Category = definition.Category,
					PermissionLabel = definition.Label,
					Selected = user?.IsPlatformSuperAdmin == true || selectedPermissions.Contains(definition.Key)
				})
				.ToList()
		};
	}

	private async Task ApplyPlatformUserPermissionsAsync(ApplicationUser user, IReadOnlyCollection<string> desiredPermissionKeys, CancellationToken cancellationToken)
	{
		var existingPermissions = await _dbContext.PlatformUserPermissions
			.Where(permission => permission.UserId == user.Id)
			.ToListAsync(cancellationToken);
		var desiredLookup = desiredPermissionKeys.ToHashSet(StringComparer.Ordinal);

		foreach (var existingPermission in existingPermissions.Where(permission => !desiredLookup.Contains(permission.PermissionKey)).ToList())
		{
			_dbContext.PlatformUserPermissions.Remove(existingPermission);
		}

		var existingLookup = existingPermissions
			.Select(permission => permission.PermissionKey)
			.ToHashSet(StringComparer.Ordinal);
		foreach (var permissionKey in desiredLookup.Where(permissionKey => !existingLookup.Contains(permissionKey)))
		{
			_dbContext.PlatformUserPermissions.Add(new PlatformUserPermission(user.Id, permissionKey));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task EnsurePlatformUserGuardrailsAsync(
		string userId,
		bool desiredEnabled,
		bool desiredSuperAdmin,
		IReadOnlyCollection<string> desiredPermissionKeys,
		CancellationToken cancellationToken)
	{
		var users = await _userManager.Users
			.AsNoTracking()
			.Include(user => user.PlatformPermissions)
			.ToListAsync(cancellationToken);

		var enabledSuperAdminCount = 0;
		var enabledOperatorCount = 0;

		foreach (var existingUser in users)
		{
			var isCurrent = string.Equals(existingUser.Id, userId, StringComparison.Ordinal);
			var isEnabled = isCurrent ? desiredEnabled : existingUser.IsPlatformUserEnabled;
			var isSuperAdmin = isCurrent ? desiredSuperAdmin : existingUser.IsPlatformSuperAdmin;
			var permissionCount = isCurrent ? desiredPermissionKeys.Count : existingUser.PlatformPermissions.Count;

			if (isEnabled && isSuperAdmin)
			{
				enabledSuperAdminCount++;
			}

			if (isEnabled && (isSuperAdmin || permissionCount > 0))
			{
				enabledOperatorCount++;
			}
		}

		if (enabledSuperAdminCount <= 0)
		{
			throw new InvalidOperationException("The final enabled platform super admin cannot be removed or disabled.");
		}

		if (enabledOperatorCount <= 0)
		{
			throw new InvalidOperationException("The final enabled platform administrator cannot be removed or disabled.");
		}
	}

	private static bool MatchesPlatformUserSearch(ApplicationUser user, string search)
	{
		var term = search.Trim();
		if (string.IsNullOrWhiteSpace(term))
		{
			return true;
		}

		if ((user.FirstName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.LastName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.UserName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
		{
			return true;
		}

		if (user.IsPlatformSuperAdmin && "super admin".Contains(term, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (user.IsPlatformUserEnabled && "enabled".Contains(term, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!user.IsPlatformUserEnabled && "disabled".Contains(term, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return user.PlatformPermissions.Any(permission =>
			permission.PermissionKey.Contains(term, StringComparison.OrdinalIgnoreCase)
			|| PlatformPermissionCatalog.Get(permission.PermissionKey).Category.Contains(term, StringComparison.OrdinalIgnoreCase)
			|| PlatformPermissionCatalog.Get(permission.PermissionKey).Label.Contains(term, StringComparison.OrdinalIgnoreCase));
	}
}
