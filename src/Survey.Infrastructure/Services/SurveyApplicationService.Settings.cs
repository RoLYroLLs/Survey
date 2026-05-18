using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<SiteSettingsEditModel> GetSiteSettingsAsync(CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantOwnerAsync(cancellationToken);

		var entity = await _dbContext.TenantSettings
			.AsNoTracking()
			.FirstOrDefaultAsync(setting => setting.TenantId == context.TenantId, cancellationToken);
		var presetKey = entity?.ThemePresetKey ?? SiteThemePresetCatalog.DefaultPresetKey;

		return new SiteSettingsEditModel
		{
			ThemePresetKey = presetKey,
			UpdatedUtc = entity?.UpdatedUtc,
			PresetOptions = SiteThemePresetCatalog.GetOptions()
		};
	}

	public async Task SaveSiteSettingsAsync(SiteSettingsEditModel model, CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantOwnerAsync(cancellationToken);

		if (!SiteThemePresetCatalog.IsValidPresetKey(model.ThemePresetKey))
		{
			throw new InvalidOperationException("The selected theme preset is not valid.");
		}

		var entity = await _dbContext.TenantSettings
			.FirstOrDefaultAsync(setting => setting.TenantId == context.TenantId, cancellationToken);

		if (entity is null)
		{
			entity = new TenantSetting(context.TenantId!.Value, model.ThemePresetKey);
			_dbContext.TenantSettings.Add(entity);
		}
		else
		{
			entity.UpdateThemePreset(model.ThemePresetKey);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		await _auditWriter.WriteAsync("tenant", "tenant.settings.changed", nameof(TenantSetting), context.TenantId!.Value.ToString(), $"Theme preset changed to '{model.ThemePresetKey}'.", true, cancellationToken);
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

		return new SiteAppearanceModel
		{
			ThemePresetKey = presetKey,
			ThemePresetName = SiteThemePresetCatalog.GetPresetName(presetKey),
			CssVariablesBlock = SiteThemePresetCatalog.BuildCssVariablesBlock(presetKey)
		};
	}
}
