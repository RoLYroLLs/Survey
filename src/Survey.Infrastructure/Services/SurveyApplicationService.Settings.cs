using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<SiteSettingsEditModel> GetSiteSettingsAsync(CancellationToken cancellationToken = default)
	{
		var entity = await _dbContext.SiteSettings
			.AsNoTracking()
			.FirstOrDefaultAsync(setting => setting.Id == SiteSetting.DefaultId, cancellationToken);
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
		if (!SiteThemePresetCatalog.IsValidPresetKey(model.ThemePresetKey))
		{
			throw new InvalidOperationException("The selected theme preset is not valid.");
		}

		var entity = await _dbContext.SiteSettings
			.FirstOrDefaultAsync(setting => setting.Id == SiteSetting.DefaultId, cancellationToken);

		if (entity is null)
		{
			entity = new SiteSetting(model.ThemePresetKey);
			_dbContext.SiteSettings.Add(entity);
		}
		else
		{
			entity.UpdateThemePreset(model.ThemePresetKey);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	public async Task<SiteAppearanceModel> GetSiteAppearanceAsync(CancellationToken cancellationToken = default)
	{
		var presetKey = await _dbContext.SiteSettings
			.AsNoTracking()
			.Where(setting => setting.Id == SiteSetting.DefaultId)
			.Select(setting => setting.ThemePresetKey)
			.FirstOrDefaultAsync(cancellationToken)
			?? SiteThemePresetCatalog.DefaultPresetKey;

		return new SiteAppearanceModel
		{
			ThemePresetKey = presetKey,
			ThemePresetName = SiteThemePresetCatalog.GetPresetName(presetKey),
			CssVariablesBlock = SiteThemePresetCatalog.BuildCssVariablesBlock(presetKey)
		};
	}
}
