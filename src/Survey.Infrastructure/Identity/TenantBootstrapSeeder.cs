using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Survey.Application.Models;
using Survey.Domain;
using Survey.Infrastructure.Persistence;
using Survey.Infrastructure.Security;

namespace Survey.Infrastructure.Identity;

internal sealed class TenantBootstrapSeeder(
	SurveyDbContext dbContext,
	UserManager<ApplicationUser> userManager,
	IConfiguration configuration,
	TenantExecutionContext tenantExecutionContext)
{
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly IConfiguration _configuration = configuration;
	private readonly TenantExecutionContext _tenantExecutionContext = tenantExecutionContext;

	public async Task SeedAsync(CancellationToken cancellationToken = default)
	{
		_tenantExecutionContext.UsePlatformBypass();

		try
		{
			if (!await _userManager.Users.AnyAsync(cancellationToken))
			{
				return;
			}

			var defaultTenant = await EnsureDefaultTenantAsync(cancellationToken);
			await BackfillTenantIdsAsync(defaultTenant.Id, cancellationToken);
			await EnsureTenantSettingAsync(defaultTenant.Id, cancellationToken);
			await EnsureMembershipsAsync(defaultTenant.Id, cancellationToken);
		}
		finally
		{
			_tenantExecutionContext.Clear();
		}
	}

	private async Task<Tenant> EnsureDefaultTenantAsync(CancellationToken cancellationToken)
	{
		var existingTenant = await _dbContext.Tenants
			.OrderBy(tenant => tenant.Id)
			.FirstOrDefaultAsync(cancellationToken);
		if (existingTenant is not null)
		{
			return existingTenant;
		}

		var tenantName = _configuration["SeedTenant:Name"]?.Trim();
		if (string.IsNullOrWhiteSpace(tenantName))
		{
			tenantName = "Imported Default Tenant";
		}

		var tenant = new Tenant(tenantName);
		_dbContext.Tenants.Add(tenant);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return tenant;
	}

	private async Task EnsureTenantSettingAsync(int tenantId, CancellationToken cancellationToken)
	{
		var exists = await _dbContext.TenantSettings
			.IgnoreQueryFilters()
			.AnyAsync(setting => setting.TenantId == tenantId, cancellationToken);
		if (exists)
		{
			return;
		}

		var presetKey = await _dbContext.SiteSettings
			.AsNoTracking()
			.Where(setting => setting.Id == SiteSetting.DefaultId)
			.Select(setting => setting.ThemePresetKey)
			.FirstOrDefaultAsync(cancellationToken)
			?? SiteThemePresetCatalog.DefaultPresetKey;

		_dbContext.TenantSettings.Add(new TenantSetting(tenantId, presetKey));
		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task EnsureMembershipsAsync(int tenantId, CancellationToken cancellationToken)
	{
		var users = await _userManager.Users
			.OrderBy(user => user.Email)
			.ToListAsync(cancellationToken);

		foreach (var user in users)
		{
			var isAdmin = await _userManager.IsInRoleAsync(user, RoleNames.Admin)
				|| await _userManager.IsInRoleAsync(user, RoleNames.PlatformSuperAdmin);

			user.IsPlatformSuperAdmin = isAdmin;
			user.IsPlatformUserEnabled = isAdmin;

			var membership = await _dbContext.TenantMemberships
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(item => item.TenantId == tenantId && item.UserId == user.Id, cancellationToken);
			if (membership is null)
			{
				membership = new TenantMembership(tenantId, user.Id, isAdmin ? TenantRole.Owner : TenantRole.User);
				_dbContext.TenantMemberships.Add(membership);
				await _dbContext.SaveChangesAsync(cancellationToken);
			}

			if (!user.ActiveTenantMembershipId.HasValue)
			{
				user.ActiveTenantMembershipId = membership.Id;
			}

			await _userManager.UpdateAsync(user);
		}
	}

	private async Task BackfillTenantIdsAsync(int tenantId, CancellationToken cancellationToken)
	{
		await BackfillTenantIdsAsync<Person>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<PersonPhone>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<PersonEmail>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<Location>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<LocationPhone>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<LocationEmail>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<PostalAddress>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<Area>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<AreaCounty>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<Goal>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<SurveyDefinition>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<SurveyVersion>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<SurveySection>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<SurveyQuestion>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<QuestionOption>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<SurveyAssignment>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<SurveyResponse>(tenantId, cancellationToken);
		await BackfillTenantIdsAsync<SurveyAnswer>(tenantId, cancellationToken);
	}

	private async Task BackfillTenantIdsAsync<TEntity>(int tenantId, CancellationToken cancellationToken)
		where TEntity : class, ITenantOwned
	{
		var items = await _dbContext.Set<TEntity>()
			.IgnoreQueryFilters()
			.Where(item => item.TenantId == 0)
			.ToListAsync(cancellationToken);

		if (items.Count == 0)
		{
			return;
		}

		foreach (var item in items)
		{
			_dbContext.Entry(item).Property(nameof(ITenantOwned.TenantId)).CurrentValue = tenantId;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}
}
