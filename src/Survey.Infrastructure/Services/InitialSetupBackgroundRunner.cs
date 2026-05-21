using Microsoft.Extensions.DependencyInjection;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

internal sealed class InitialSetupBackgroundRunner(
	InitialSetupSeeder initialSetupSeeder,
	IServiceScopeFactory serviceScopeFactory)
{
	private readonly InitialSetupSeeder _initialSetupSeeder = initialSetupSeeder;
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

	public async Task RunAsync(int operationId, string[] selectedThemeKeys, string defaultThemeKey, bool resetBeforeRun, CancellationToken cancellationToken)
	{
		await UseBackgroundOperationsServiceAsync(
			service => service.MarkRunningAsync(
				operationId,
				resetBeforeRun ? "Restarting initial setup seeding from a clean slate." : "Starting initial setup seeding.",
				cancellationToken));

		try
		{
			if (resetBeforeRun)
			{
				await _initialSetupSeeder.ResetSeededDataAsync(cancellationToken);
			}

			await _initialSetupSeeder.SeedAsync(selectedThemeKeys, defaultThemeKey, update =>
				UseBackgroundOperationsServiceAsync(service =>
					service.ReportInitialSetupProgressAsync(operationId, update, cancellationToken)), cancellationToken);
			await UseBackgroundOperationsServiceAsync(
				service => service.CompleteOperationAsync(operationId, "Initial setup completed.", cancellationToken));
		}
		catch (Exception ex)
		{
			await UseBackgroundOperationsServiceAsync(
				service => service.FailOperationAsync(operationId, ex.Message, cancellationToken));
			throw;
		}
	}

	private async Task UseBackgroundOperationsServiceAsync(Func<BackgroundOperationsService, Task> action)
	{
		using var scope = _serviceScopeFactory.CreateScope();
		var service = scope.ServiceProvider.GetRequiredService<BackgroundOperationsService>();
		await action(service);
	}
}
