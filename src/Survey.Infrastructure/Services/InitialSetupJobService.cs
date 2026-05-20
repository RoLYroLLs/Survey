using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Configuration;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

public sealed class InitialSetupJobService(
	SurveyDbContext dbContext,
	BackgroundOperationsService backgroundOperationsService,
	InitialSetupTaskQueue initialSetupTaskQueue,
	IOptions<BackgroundJobsOptions> options) : IInitialSetupJobService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly BackgroundOperationsService _backgroundOperationsService = backgroundOperationsService;
	private readonly InitialSetupTaskQueue _initialSetupTaskQueue = initialSetupTaskQueue;
	private readonly BackgroundJobsOptions _options = options.Value;

	public async Task<InitialSetupJobStartResult> StartOrResumeAsync(
		IReadOnlyCollection<string> selectedThemeKeys,
		string defaultThemeKey,
		string? requestedByUserId,
		CancellationToken cancellationToken = default)
	{
		var activeOperation = await _dbContext.BackgroundOperations
			.AsNoTracking()
			.Where(operation => operation.Kind == BackgroundOperationKinds.InitialSetupSeeding
				&& (operation.Status == BackgroundOperationStatuses.Queued || operation.Status == BackgroundOperationStatuses.Running))
			.OrderByDescending(operation => operation.Id)
			.FirstOrDefaultAsync(cancellationToken);
		if (activeOperation is not null)
		{
			return new InitialSetupJobStartResult
			{
				OperationId = activeOperation.Id,
				Status = activeOperation.Status,
				AlreadyRunning = true
			};
		}

		return await QueueInitialSetupAsync(selectedThemeKeys, defaultThemeKey, requestedByUserId, cancellationToken);
	}

	public Task<InitialSetupJobStartResult> RetryAsync(
		IReadOnlyCollection<string> selectedThemeKeys,
		string defaultThemeKey,
		string? requestedByUserId,
		CancellationToken cancellationToken = default)
	{
		return QueueInitialSetupAsync(selectedThemeKeys, defaultThemeKey, requestedByUserId, cancellationToken);
	}

	private async Task<InitialSetupJobStartResult> QueueInitialSetupAsync(
		IReadOnlyCollection<string> selectedThemeKeys,
		string defaultThemeKey,
		string? requestedByUserId,
		CancellationToken cancellationToken)
	{
		var normalizedSelectedThemeKeys = selectedThemeKeys
			.Where(static key => !string.IsNullOrWhiteSpace(key))
			.Select(static key => key.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		var normalizedDefaultThemeKey = defaultThemeKey?.Trim() ?? string.Empty;
		if (normalizedSelectedThemeKeys.Length == 0)
		{
			throw new InvalidOperationException("Select at least one theme before starting setup.");
		}

		var metadataJson = JsonSerializer.Serialize(new InitialSetupOperationMetadata
		{
			DefaultThemeKey = normalizedDefaultThemeKey,
			SelectedThemeKeys = normalizedSelectedThemeKeys
		}, JsonOptions);
		var stageStatesJson = BackgroundOperationsService.SerializeStageSnapshots(BackgroundOperationsService.CreateDefaultStageSnapshots());
		var operationId = await _backgroundOperationsService.CreateOperationAsync(
			BackgroundOperationKinds.InitialSetupSeeding,
			_options.SetupQueueName,
			"Initial setup seeding",
			null,
			requestedByUserId,
			metadataJson,
			stageStatesJson,
			cancellationToken);
		await _initialSetupTaskQueue.QueueAsync(new InitialSetupWorkItem
		{
			OperationId = operationId,
			SelectedThemeKeys = normalizedSelectedThemeKeys,
			DefaultThemeKey = normalizedDefaultThemeKey
		}, cancellationToken);

		return new InitialSetupJobStartResult
		{
			OperationId = operationId,
			Status = BackgroundOperationStatuses.Queued
		};
	}
}
