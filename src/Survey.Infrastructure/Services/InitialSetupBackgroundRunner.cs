using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

internal sealed class InitialSetupBackgroundRunner(
	InitialSetupSeeder initialSetupSeeder,
	BackgroundOperationsService backgroundOperationsService)
{
	private readonly InitialSetupSeeder _initialSetupSeeder = initialSetupSeeder;
	private readonly BackgroundOperationsService _backgroundOperationsService = backgroundOperationsService;

	public async Task RunAsync(int operationId, string[] selectedThemeKeys, string defaultThemeKey, CancellationToken cancellationToken)
	{
		await _backgroundOperationsService.MarkRunningAsync(operationId, "Starting initial setup seeding.", cancellationToken);

		try
		{
			await _initialSetupSeeder.SeedAsync(selectedThemeKeys, defaultThemeKey, update =>
				_backgroundOperationsService.ReportInitialSetupProgressAsync(operationId, update, cancellationToken), cancellationToken);
			await _backgroundOperationsService.CompleteOperationAsync(operationId, "Initial setup completed.", cancellationToken);
		}
		catch (Exception ex)
		{
			await _backgroundOperationsService.FailOperationAsync(operationId, ex.Message, cancellationToken);
			throw;
		}
	}
}
