using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<SiteSettingsEditModel> GetSiteSettingsAsync(CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantAdminOrOwnerAsync(cancellationToken);
		var options = await GetTenantThemeOptionsAsync(cancellationToken);
		var tenant = await _dbContext.Tenants
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Id == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The active tenant could not be found.");

		var entity = await _dbContext.TenantSettings
			.AsNoTracking()
			.FirstOrDefaultAsync(setting => setting.TenantId == context.TenantId, cancellationToken);
		var presetKey = entity?.ThemePresetKey ?? SiteThemePresetCatalog.DefaultPresetKey;

		return new SiteSettingsEditModel
		{
			TenantName = tenant.Name,
			ThemePresetKey = presetKey,
			UpdatedUtc = entity?.UpdatedUtc,
			PresetOptions = options,
			CanManageTheme = context.TenantRole is TenantRole.Owner or TenantRole.Admin
		};
	}

	public async Task SaveSiteSettingsAsync(SiteSettingsEditModel model, CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantAdminOrOwnerAsync(cancellationToken);
		var tenant = await _dbContext.Tenants
			.FirstOrDefaultAsync(item => item.Id == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The active tenant could not be found.");
		var originalTenantName = tenant.Name;

		if (string.IsNullOrWhiteSpace(model.TenantName))
		{
			throw new InvalidOperationException("The tenant name is required.");
		}

		tenant.Update(model.TenantName);

		var ownerUserId = await _dbContext.TenantMemberships
			.AsNoTracking()
			.Where(membership => membership.TenantId == tenant.Id && membership.Role == TenantRole.Owner)
			.Select(membership => membership.UserId)
			.FirstOrDefaultAsync(cancellationToken);
		if (!string.IsNullOrWhiteSpace(ownerUserId)
			&& await TenantNameOwnedByUserExistsAsync(ownerUserId, tenant.Slug, tenant.Id, cancellationToken))
		{
			throw new InvalidOperationException("This tenant owner already owns another tenant with that name. Please choose a different tenant name.");
		}

		var auditDetails = new List<string>();
		if (!string.Equals(originalTenantName, tenant.Name, StringComparison.Ordinal))
		{
			auditDetails.Add($"Tenant renamed to '{tenant.Name}'.");
		}

		if (context.TenantRole is TenantRole.Owner or TenantRole.Admin)
		{
			if (!SiteThemePresetCatalog.IsValidPresetKey(model.ThemePresetKey))
			{
				var exists = await _dbContext.PlatformThemes
					.AsNoTracking()
					.AnyAsync(theme => theme.Key == model.ThemePresetKey && theme.IsEnabled && !theme.IsArchived, cancellationToken);
				if (!exists)
				{
					throw new InvalidOperationException("The selected theme preset is not valid.");
				}
			}

			var entity = await _dbContext.TenantSettings
				.FirstOrDefaultAsync(setting => setting.TenantId == context.TenantId, cancellationToken);

			if (entity is null)
			{
				entity = new TenantSetting(context.TenantId!.Value, model.ThemePresetKey);
				_dbContext.TenantSettings.Add(entity);
			}

			if (!string.Equals(entity.ThemePresetKey, model.ThemePresetKey, StringComparison.OrdinalIgnoreCase))
			{
				entity.UpdateThemePreset(model.ThemePresetKey);
				auditDetails.Add($"Theme preset changed to '{model.ThemePresetKey}'.");
			}
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		if (auditDetails.Count > 0)
		{
			await _auditWriter.WriteAsync("tenant", "tenant.settings.changed", nameof(Tenant), context.TenantId!.Value.ToString(), string.Join(" ", auditDetails), true, cancellationToken);
		}
	}

	public async Task<IReadOnlyList<ThemePresetOption>> GetTenantThemeOptionsAsync(CancellationToken cancellationToken = default)
	{
		var options = await _dbContext.PlatformThemes
			.AsNoTracking()
			.Where(theme => theme.IsEnabled && !theme.IsArchived)
			.OrderBy(theme => theme.Name)
			.ThenBy(theme => theme.Key)
			.Select(theme => new ThemePresetOption
			{
				Key = theme.Key,
				Name = theme.Name,
				Description = theme.Description,
				PrimaryColor = theme.PrimaryColor,
				AccentColor = theme.AccentColor,
				BackgroundColor = theme.BackgroundColor,
				PreviewStyle = $"background: linear-gradient(135deg, {theme.PrimaryColor}, {theme.AccentColor});"
			})
			.ToListAsync(cancellationToken);

		return options.Count > 0
			? options
			: SiteThemePresetCatalog.GetOptions();
	}

	public async Task<SiteAppearanceModel> GetSiteAppearanceAsync(CancellationToken cancellationToken = default)
	{
		var context = await _tenantContextAccessor.GetCurrentAsync(cancellationToken);
		var presetKey = context.TenantId.HasValue
			? await _dbContext.TenantSettings
				.AsNoTracking()
				.Where(setting => setting.TenantId == context.TenantId.Value)
				.Select(setting => setting.ThemePresetKey)
				.FirstOrDefaultAsync(cancellationToken)
			: null;

		presetKey ??= await _dbContext.SiteSettings
			.AsNoTracking()
			.Where(setting => setting.Id == SiteSetting.DefaultId)
			.Select(setting => setting.ThemePresetKey)
			.FirstOrDefaultAsync(cancellationToken);
		presetKey ??= SiteThemePresetCatalog.DefaultPresetKey;
		var theme = await _dbContext.PlatformThemes
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Key == presetKey, cancellationToken);

		return new SiteAppearanceModel
		{
			ThemePresetKey = presetKey,
			ThemePresetName = theme?.Name ?? SiteThemePresetCatalog.GetPresetName(presetKey),
			CssVariablesBlock = theme?.CssVariablesBlock ?? SiteThemePresetCatalog.BuildCssVariablesBlock(presetKey)
		};
	}

	private Task<bool> TenantNameOwnedByUserExistsAsync(string userId, string slug, int? excludedTenantId, CancellationToken cancellationToken)
	{
		return _dbContext.TenantMemberships
			.AsNoTracking()
			.AnyAsync(
				membership => membership.UserId == userId
					&& membership.Role == TenantRole.Owner
					&& membership.Tenant.Slug == slug
					&& (!excludedTenantId.HasValue || membership.TenantId != excludedTenantId.Value),
				cancellationToken);
	}
}
