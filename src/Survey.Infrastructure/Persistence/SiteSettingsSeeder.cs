using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Persistence;

internal sealed class SiteSettingsSeeder(SurveyDbContext dbContext)
{
	private readonly SurveyDbContext _dbContext = dbContext;

	public async Task SeedAsync(CancellationToken cancellationToken = default)
	{
		await SeedPlatformThemesAsync(cancellationToken);

		var entity = await _dbContext.SiteSettings
			.FirstOrDefaultAsync(setting => setting.Id == SiteSetting.DefaultId, cancellationToken);

		if (entity is null)
		{
			_dbContext.SiteSettings.Add(new SiteSetting(SiteThemePresetCatalog.DefaultPresetKey));
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		var themeExists = await _dbContext.PlatformThemes
			.AsNoTracking()
			.AnyAsync(theme => theme.Key == entity.ThemePresetKey, cancellationToken);
		if (!themeExists)
		{
			entity.UpdateThemePreset(SiteThemePresetCatalog.DefaultPresetKey);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}
	}

	private async Task SeedPlatformThemesAsync(CancellationToken cancellationToken)
	{
		var existingKeys = await _dbContext.PlatformThemes
			.AsNoTracking()
			.Select(theme => theme.Key)
			.ToListAsync(cancellationToken);
		var existingLookup = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var seededAny = false;

		foreach (var seed in SiteThemePresetCatalog.GetSeedModels().Where(seed => !existingLookup.Contains(seed.Key)))
		{
			_dbContext.PlatformThemes.Add(new PlatformTheme(
				seed.Key,
				seed.Name,
				seed.Description,
				seed.PrimaryColor,
				seed.AccentColor,
				seed.BackgroundColor,
				seed.CssVariablesBlock));
			seededAny = true;
		}

		if (seededAny)
		{
			await _dbContext.SaveChangesAsync(cancellationToken);
		}
	}
}
