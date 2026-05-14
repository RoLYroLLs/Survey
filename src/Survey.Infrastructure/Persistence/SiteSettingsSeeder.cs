using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Persistence;

internal sealed class SiteSettingsSeeder(SurveyDbContext dbContext)
{
	private readonly SurveyDbContext _dbContext = dbContext;

	public async Task SeedAsync(CancellationToken cancellationToken = default)
	{
		var entity = await _dbContext.SiteSettings
			.FirstOrDefaultAsync(setting => setting.Id == SiteSetting.DefaultId, cancellationToken);

		if (entity is null)
		{
			_dbContext.SiteSettings.Add(new SiteSetting(SiteThemePresetCatalog.DefaultPresetKey));
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		if (!SiteThemePresetCatalog.IsValidPresetKey(entity.ThemePresetKey))
		{
			entity.UpdateThemePreset(SiteThemePresetCatalog.DefaultPresetKey);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}
	}
}
