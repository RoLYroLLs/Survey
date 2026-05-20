using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Persistence;

public sealed class InitialSetupSeeder(
	InitialSetupStateService initialSetupStateService,
	InitialSeedingProgressService initialSeedingProgressService,
	SurveyDbContext dbContext,
	IdentityDataSeeder identityDataSeeder,
	SiteSettingsSeeder siteSettingsSeeder,
	GeographyDataSeeder geographyDataSeeder)
{
	private readonly InitialSetupStateService _initialSetupStateService = initialSetupStateService;
	private readonly InitialSeedingProgressService _initialSeedingProgressService = initialSeedingProgressService;
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly IdentityDataSeeder _identityDataSeeder = identityDataSeeder;
	private readonly SiteSettingsSeeder _siteSettingsSeeder = siteSettingsSeeder;
	private readonly GeographyDataSeeder _geographyDataSeeder = geographyDataSeeder;

	public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
	{
		var setupState = await _initialSetupStateService.GetStatusAsync(cancellationToken);
		return setupState.IsSeeded;
	}

	public async Task SeedAsync(
		IReadOnlyCollection<string> selectedThemeKeys,
		string defaultThemeKey,
		Func<InitialSeedingProgressUpdate, Task>? reportProgress = null,
		CancellationToken cancellationToken = default)
	{
		_initialSeedingProgressService.Start();

		try
		{
			async Task ForwardProgressAsync(InitialSeedingProgressUpdate update)
			{
				_initialSeedingProgressService.Report(update);
				if (reportProgress is not null)
				{
					await reportProgress(update);
				}
			}

			await _identityDataSeeder.SeedAsync(ForwardProgressAsync, cancellationToken);
			await _geographyDataSeeder.SeedAsync(reportProgress: ForwardProgressAsync, cancellationToken: cancellationToken);
			await _siteSettingsSeeder.SeedAsync(selectedThemeKeys, defaultThemeKey, ForwardProgressAsync, cancellationToken);
			_initialSetupStateService.SetStatus(hasUsers: true, isSeeded: true, isComplete: false);
			_initialSeedingProgressService.Complete();
		}
		catch (Exception ex)
		{
			_initialSeedingProgressService.Fail(ex.Message);
			throw;
		}
	}

	public async Task ResetSeededDataAsync(CancellationToken cancellationToken = default)
	{
		_initialSeedingProgressService.Reset();

		_dbContext.SiteSettings.RemoveRange(await _dbContext.SiteSettings.ToListAsync(cancellationToken));
		_dbContext.PlatformThemes.RemoveRange(await _dbContext.PlatformThemes.ToListAsync(cancellationToken));
		_dbContext.ZipCountyLookups.RemoveRange(await _dbContext.ZipCountyLookups.ToListAsync(cancellationToken));
		_dbContext.Counties.RemoveRange(await _dbContext.Counties.ToListAsync(cancellationToken));
		_dbContext.StateProvinces.RemoveRange(await _dbContext.StateProvinces.ToListAsync(cancellationToken));
		_dbContext.Countries.RemoveRange(await _dbContext.Countries.ToListAsync(cancellationToken));
		_dbContext.SeedStates.RemoveRange(await _dbContext.SeedStates.ToListAsync(cancellationToken));

		await _dbContext.SaveChangesAsync(cancellationToken);
		_initialSetupStateService.SetStatus(hasUsers: true, isSeeded: false, isComplete: false);
	}

	public async Task MarkSetupCompleteAsync(CancellationToken cancellationToken = default)
	{
		var siteSettings = await _dbContext.SiteSettings
			.FirstOrDefaultAsync(setting => setting.Id == SiteSetting.DefaultId, cancellationToken)
			?? throw new InvalidOperationException("Initial setup cannot be completed before site settings have been seeded.");

		siteSettings.MarkInitialSetupComplete();
		await _dbContext.SaveChangesAsync(cancellationToken);
		_initialSetupStateService.SetStatus(hasUsers: true, isSeeded: true, isComplete: true);
	}
}
