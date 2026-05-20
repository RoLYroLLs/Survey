using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Persistence;

public sealed class SiteSettingsSeeder(SurveyDbContext dbContext)
{
	private readonly SurveyDbContext _dbContext = dbContext;

	public Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
	{
		return _dbContext.SiteSettings
			.AsNoTracking()
			.Where(setting => setting.Id == SiteSetting.DefaultId)
			.Join(
				_dbContext.PlatformThemes.AsNoTracking(),
				setting => setting.ThemePresetKey,
				theme => theme.Key,
				(setting, _) => setting.Id)
			.AnyAsync(cancellationToken);
	}

	public Task<bool> IsInitialSetupCompletedAsync(CancellationToken cancellationToken = default)
	{
		return _dbContext.SiteSettings
			.AsNoTracking()
			.Where(setting => setting.Id == SiteSetting.DefaultId && setting.InitialSetupCompletedUtc.HasValue)
			.AnyAsync(cancellationToken);
	}

	public async Task SeedAsync(
		IReadOnlyCollection<string> selectedThemeKeys,
		string defaultThemeKey,
		Func<InitialSeedingProgressUpdate, Task>? reportProgress = null,
		CancellationToken cancellationToken = default)
	{
		var normalizedSelectedKeys = selectedThemeKeys
			.Where(static key => !string.IsNullOrWhiteSpace(key))
			.Select(static key => key.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var selectedSeeds = SiteThemePresetCatalog.GetSeedModels()
			.Where(seed => normalizedSelectedKeys.Contains(seed.Key))
			.ToList();

		if (selectedSeeds.Count == 0)
		{
			throw new InvalidOperationException("Select at least one predefined theme before starting the initial setup.");
		}

		var normalizedDefaultThemeKey = defaultThemeKey?.Trim() ?? string.Empty;
		var defaultTheme = selectedSeeds.FirstOrDefault(seed => string.Equals(seed.Key, normalizedDefaultThemeKey, StringComparison.OrdinalIgnoreCase))
			?? throw new InvalidOperationException("Choose a default theme from the selected predefined themes before starting the initial setup.");

		await SeedPlatformThemesAsync(selectedSeeds, normalizedSelectedKeys, reportProgress, cancellationToken);
		await SeedSiteSettingsAsync(defaultTheme, reportProgress, cancellationToken);
	}

	private async Task SeedPlatformThemesAsync(
		IReadOnlyList<ThemeSeedModel> selectedSeeds,
		IReadOnlySet<string> selectedKeys,
		Func<InitialSeedingProgressUpdate, Task>? reportProgress,
		CancellationToken cancellationToken)
	{
		var existingThemes = await _dbContext.PlatformThemes
			.ToListAsync(cancellationToken);
		var existingLookup = existingThemes.ToDictionary(theme => theme.Key, StringComparer.OrdinalIgnoreCase);
		var processed = 0;
		var total = selectedSeeds.Count;

		await ReportPlatformThemesProgressAsync(reportProgress, processed, total, isComplete: false, activityMessage: "Preparing selected platform themes.");

		foreach (var theme in existingThemes.Where(theme => !selectedKeys.Contains(theme.Key)).ToList())
		{
			_dbContext.PlatformThemes.Remove(theme);
		}

		foreach (var seed in selectedSeeds)
		{
			var activityMessage = $"Adding theme '{seed.Name}'.";
			if (existingLookup.TryGetValue(seed.Key, out var existing))
			{
				existing.UpdateIdentity(seed.Key, seed.Name, seed.Description);
				existing.UpdatePresentation(seed.PrimaryColor, seed.AccentColor, seed.BackgroundColor, seed.CssVariablesBlock);
				existing.Enable();
			}
			else
			{
				_dbContext.PlatformThemes.Add(new PlatformTheme(
					seed.Key,
					seed.Name,
					seed.Description,
					seed.PrimaryColor,
					seed.AccentColor,
					seed.BackgroundColor,
					seed.CssVariablesBlock));
			}

			processed++;
			await ReportPlatformThemesProgressAsync(reportProgress, processed, total, isComplete: processed == total, activityMessage);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task SeedSiteSettingsAsync(
		ThemeSeedModel defaultTheme,
		Func<InitialSeedingProgressUpdate, Task>? reportProgress,
		CancellationToken cancellationToken)
	{
		var activityMessage = $"Setting default theme to '{defaultTheme.Name}'.";
		await ReportSiteSettingsProgressAsync(reportProgress, 0, 1, isComplete: false, activityMessage);

		var entity = await _dbContext.SiteSettings
			.FirstOrDefaultAsync(setting => setting.Id == SiteSetting.DefaultId, cancellationToken);

		if (entity is null)
		{
			_dbContext.SiteSettings.Add(new SiteSetting(defaultTheme.Key));
		}
		else
		{
			entity.UpdateThemePreset(defaultTheme.Key);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		await ReportSiteSettingsProgressAsync(reportProgress, 1, 1, isComplete: true, activityMessage);
	}

	private static Task ReportPlatformThemesProgressAsync(
		Func<InitialSeedingProgressUpdate, Task>? reportProgress,
		int processed,
		int total,
		bool isComplete,
		string activityMessage)
	{
		if (reportProgress is null)
		{
			return Task.CompletedTask;
		}

		return reportProgress(new InitialSeedingProgressUpdate
		{
			StageKey = InitialSeedingStages.PlatformThemes,
			StageLabel = InitialSeedingStages.GetLabel(InitialSeedingStages.PlatformThemes),
			ActivityMessage = activityMessage,
			Processed = processed,
			Total = total,
			IsComplete = isComplete
		});
	}

	private static Task ReportSiteSettingsProgressAsync(
		Func<InitialSeedingProgressUpdate, Task>? reportProgress,
		int processed,
		int total,
		bool isComplete,
		string activityMessage)
	{
		if (reportProgress is null)
		{
			return Task.CompletedTask;
		}

		return reportProgress(new InitialSeedingProgressUpdate
		{
			StageKey = InitialSeedingStages.SiteSettings,
			StageLabel = InitialSeedingStages.GetLabel(InitialSeedingStages.SiteSettings),
			ActivityMessage = activityMessage,
			Processed = processed,
			Total = total,
			IsComplete = isComplete
		});
	}
}
