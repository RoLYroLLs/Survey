using Survey.Application.Models;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Persistence;

public sealed class InitialSetupSeeder(
	InitialSetupStateService initialSetupStateService,
	InitialSeedingProgressService initialSeedingProgressService,
	IdentityDataSeeder identityDataSeeder,
	SiteSettingsSeeder siteSettingsSeeder,
	GeographyDataSeeder geographyDataSeeder)
{
	private readonly InitialSetupStateService _initialSetupStateService = initialSetupStateService;
	private readonly InitialSeedingProgressService _initialSeedingProgressService = initialSeedingProgressService;
	private readonly IdentityDataSeeder _identityDataSeeder = identityDataSeeder;
	private readonly SiteSettingsSeeder _siteSettingsSeeder = siteSettingsSeeder;
	private readonly GeographyDataSeeder _geographyDataSeeder = geographyDataSeeder;

	public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
	{
		var setupState = await _initialSetupStateService.GetStatusAsync(cancellationToken);
		return setupState.IsComplete;
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
			_initialSetupStateService.SetStatus(hasUsers: true, isComplete: true);
			_initialSeedingProgressService.Complete();
		}
		catch (Exception ex)
		{
			_initialSeedingProgressService.Fail(ex.Message);
			throw;
		}
	}
}
