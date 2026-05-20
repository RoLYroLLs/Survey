using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Survey.Infrastructure.Services;

internal sealed class InitialSetupBackgroundWorker(
	InitialSetupTaskQueue taskQueue,
	IServiceScopeFactory serviceScopeFactory,
	ILogger<InitialSetupBackgroundWorker> logger) : BackgroundService
{
	private readonly InitialSetupTaskQueue _taskQueue = taskQueue;
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
	private readonly ILogger<InitialSetupBackgroundWorker> _logger = logger;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await RequeueRecoverableOperationsAsync(stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			InitialSetupWorkItem workItem;
			try
			{
				workItem = await _taskQueue.DequeueAsync(stoppingToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}

			using var scope = _serviceScopeFactory.CreateScope();
			var runner = scope.ServiceProvider.GetRequiredService<InitialSetupBackgroundRunner>();

			try
			{
				await runner.RunAsync(workItem.OperationId, workItem.SelectedThemeKeys, workItem.DefaultThemeKey, workItem.ResetBeforeRun, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Initial setup background work item {OperationId} failed.", workItem.OperationId);
			}
		}
	}

	private async Task RequeueRecoverableOperationsAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceScopeFactory.CreateScope();
		var jobService = scope.ServiceProvider.GetRequiredService<InitialSetupJobService>();

		try
		{
			await jobService.ResumePendingOperationsAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unable to requeue recoverable initial setup operations on startup.");
		}
	}
}
