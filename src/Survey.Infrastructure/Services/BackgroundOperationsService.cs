using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

public sealed class BackgroundOperationsService(
	SurveyDbContext dbContext,
	IPlatformPermissionEvaluator platformPermissionEvaluator,
	InitialSetupStateService initialSetupStateService,
	InitialSeedingProgressService initialSeedingProgressService) : IBackgroundOperationsService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly IPlatformPermissionEvaluator _platformPermissionEvaluator = platformPermissionEvaluator;
	private readonly InitialSetupStateService _initialSetupStateService = initialSetupStateService;
	private readonly InitialSeedingProgressService _initialSeedingProgressService = initialSeedingProgressService;

	public async Task<PagedResult<BackgroundOperationListItem>> GetBackgroundOperationsAsync(
		PagedQuery request,
		string? kind = null,
		string? status = null,
		int? tenantId = null,
		string? search = null,
		CancellationToken cancellationToken = default)
	{
		await _platformPermissionEvaluator.EnsurePermissionAsync(PlatformPermissionKeys.JobsView, cancellationToken);

		var normalizedRequest = request.Normalize();
		var normalizedSearch = search?.Trim();
		var query = _dbContext.BackgroundOperations.AsNoTracking();

		if (!string.IsNullOrWhiteSpace(kind))
		{
			var normalizedKind = kind.Trim();
			query = query.Where(operation => operation.Kind == normalizedKind);
		}

		if (!string.IsNullOrWhiteSpace(status))
		{
			var normalizedStatus = status.Trim();
			query = query.Where(operation => operation.Status == normalizedStatus);
		}

		if (tenantId.HasValue)
		{
			query = query.Where(operation => operation.TenantId == tenantId.Value);
		}

		if (!string.IsNullOrWhiteSpace(normalizedSearch))
		{
			query = query.Where(operation =>
				operation.Summary.Contains(normalizedSearch)
				|| operation.CurrentItemMessage.Contains(normalizedSearch));
		}

		var totalCount = await query.CountAsync(cancellationToken);
		var operations = await query
			.OrderByDescending(operation => operation.Id)
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToListAsync(cancellationToken);
		var tenantNames = await LoadTenantNamesAsync(operations.Where(item => item.TenantId.HasValue).Select(item => item.TenantId!.Value).Distinct().ToArray(), cancellationToken);
		var userDisplayNames = await LoadUserDisplayNamesAsync(operations.Where(item => !string.IsNullOrWhiteSpace(item.RequestedByUserId)).Select(item => item.RequestedByUserId!).Distinct().ToArray(), cancellationToken);
		var items = operations
			.Select(operation => new BackgroundOperationListItem
			{
				Id = operation.Id,
				Kind = operation.Kind,
				Status = operation.Status,
				Summary = operation.Summary,
				QueueName = operation.QueueName,
				HangfireJobId = operation.HangfireJobId,
				ProgressPercent = operation.ProgressPercent,
				CurrentStageLabel = operation.CurrentStageLabel,
				CurrentItemMessage = operation.CurrentItemMessage,
				TenantId = operation.TenantId,
				TenantName = operation.TenantId.HasValue ? tenantNames.GetValueOrDefault(operation.TenantId.Value) ?? string.Empty : string.Empty,
				RequestedByDisplayName = !string.IsNullOrWhiteSpace(operation.RequestedByUserId) ? userDisplayNames.GetValueOrDefault(operation.RequestedByUserId) ?? string.Empty : string.Empty,
				CreatedUtc = operation.CreatedUtc,
				StartedUtc = operation.StartedUtc,
				CompletedUtc = operation.CompletedUtc
			})
			.ToList();

		return new PagedResult<BackgroundOperationListItem>
		{
			Items = items,
			TotalCount = totalCount,
			HasMore = normalizedRequest.Offset + items.Count < totalCount
		};
	}

	public async Task<BackgroundOperationDetailModel?> GetBackgroundOperationAsync(int id, CancellationToken cancellationToken = default)
	{
		await _platformPermissionEvaluator.EnsurePermissionAsync(PlatformPermissionKeys.JobsView, cancellationToken);
		return await GetBackgroundOperationCoreAsync(id, cancellationToken);
	}

	public async Task<PagedResult<OutboundEmailListItem>> GetOutboundEmailsAsync(
		PagedQuery request,
		string? status = null,
		int? tenantId = null,
		string? sourceType = null,
		string? search = null,
		CancellationToken cancellationToken = default)
	{
		await _platformPermissionEvaluator.EnsurePermissionAsync(PlatformPermissionKeys.JobsView, cancellationToken);

		var normalizedRequest = request.Normalize();
		var normalizedSearch = search?.Trim();
		var query = _dbContext.OutboundEmails.AsNoTracking();

		if (!string.IsNullOrWhiteSpace(status))
		{
			var normalizedStatus = status.Trim();
			query = query.Where(email => email.Status == normalizedStatus);
		}

		if (tenantId.HasValue)
		{
			query = query.Where(email => email.TenantId == tenantId.Value);
		}

		if (!string.IsNullOrWhiteSpace(sourceType))
		{
			var normalizedSourceType = sourceType.Trim();
			query = query.Where(email => email.SourceType == normalizedSourceType);
		}

		if (!string.IsNullOrWhiteSpace(normalizedSearch))
		{
			query = query.Where(email =>
				email.RecipientEmail.Contains(normalizedSearch)
				|| email.RecipientName.Contains(normalizedSearch)
				|| email.Subject.Contains(normalizedSearch));
		}

		var totalCount = await query.CountAsync(cancellationToken);
		var emails = await query
			.OrderByDescending(email => email.Id)
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToListAsync(cancellationToken);
		var tenantNames = await LoadTenantNamesAsync(emails.Where(item => item.TenantId.HasValue).Select(item => item.TenantId!.Value).Distinct().ToArray(), cancellationToken);
		var items = emails
			.Select(email => new OutboundEmailListItem
			{
				Id = email.Id,
				RecipientEmail = email.RecipientEmail,
				RecipientName = email.RecipientName,
				Subject = email.Subject,
				TemplateKey = email.TemplateKey,
				SourceType = email.SourceType,
				SourceId = email.SourceId,
				Status = email.Status,
				ProviderMessageId = email.ProviderMessageId ?? string.Empty,
				AttemptCount = email.AttemptCount,
				OpenCount = email.OpenCount,
				ClickCount = email.ClickCount,
				TenantId = email.TenantId,
				TenantName = email.TenantId.HasValue ? tenantNames.GetValueOrDefault(email.TenantId.Value) ?? string.Empty : string.Empty,
				CreatedUtc = email.CreatedUtc,
				SentUtc = email.SentUtc,
				FirstOpenedUtc = email.FirstOpenedUtc,
				LastOpenedUtc = email.LastOpenedUtc,
				FirstClickedUtc = email.FirstClickedUtc,
				LastClickedUtc = email.LastClickedUtc
			})
			.ToList();

		return new PagedResult<OutboundEmailListItem>
		{
			Items = items,
			TotalCount = totalCount,
			HasMore = normalizedRequest.Offset + items.Count < totalCount
		};
	}

	public async Task<OutboundEmailDetailModel?> GetOutboundEmailAsync(int id, CancellationToken cancellationToken = default)
	{
		await _platformPermissionEvaluator.EnsurePermissionAsync(PlatformPermissionKeys.JobsView, cancellationToken);

		var entity = await _dbContext.OutboundEmails
			.AsNoTracking()
			.Include(email => email.Attempts)
			.Include(email => email.ClickEvents)
			.FirstOrDefaultAsync(email => email.Id == id, cancellationToken);
		if (entity is null)
		{
			return null;
		}

		var tenantNames = await LoadTenantNamesAsync(entity.TenantId.HasValue ? [entity.TenantId.Value] : [], cancellationToken);
		var userDisplayNames = await LoadUserDisplayNamesAsync(!string.IsNullOrWhiteSpace(entity.CreatedByUserId) ? [entity.CreatedByUserId!] : [], cancellationToken);

		return new OutboundEmailDetailModel
		{
			Id = entity.Id,
			BackgroundOperationId = entity.BackgroundOperationId,
			RecipientEmail = entity.RecipientEmail,
			RecipientName = entity.RecipientName,
			Subject = entity.Subject,
			HtmlBody = entity.HtmlBody,
			TextBody = entity.TextBody,
			TemplateKey = entity.TemplateKey,
			SourceType = entity.SourceType,
			SourceId = entity.SourceId,
			TrackingToken = entity.TrackingToken,
			Status = entity.Status,
			ProviderMessageId = entity.ProviderMessageId ?? string.Empty,
			LastError = entity.LastError ?? string.Empty,
			AttemptCount = entity.AttemptCount,
			OpenCount = entity.OpenCount,
			ClickCount = entity.ClickCount,
			TenantId = entity.TenantId,
			TenantName = entity.TenantId.HasValue ? tenantNames.GetValueOrDefault(entity.TenantId.Value) ?? string.Empty : string.Empty,
			RequestedByDisplayName = !string.IsNullOrWhiteSpace(entity.CreatedByUserId) ? userDisplayNames.GetValueOrDefault(entity.CreatedByUserId) ?? string.Empty : string.Empty,
			CreatedUtc = entity.CreatedUtc,
			UpdatedUtc = entity.UpdatedUtc,
			SentUtc = entity.SentUtc,
			FirstOpenedUtc = entity.FirstOpenedUtc,
			LastOpenedUtc = entity.LastOpenedUtc,
			FirstClickedUtc = entity.FirstClickedUtc,
			LastClickedUtc = entity.LastClickedUtc,
			Attempts = entity.Attempts
				.OrderBy(item => item.AttemptNumber)
				.Select(item => new OutboundEmailAttemptListItem
				{
					Id = item.Id,
					AttemptNumber = item.AttemptNumber,
					Status = item.Status,
					ProviderMessageId = item.ProviderMessageId ?? string.Empty,
					ErrorMessage = item.ErrorMessage ?? string.Empty,
					StartedUtc = item.StartedUtc,
					CompletedUtc = item.CompletedUtc
				})
				.ToList(),
			ClickEvents = entity.ClickEvents
				.OrderByDescending(item => item.OccurredUtc)
				.Select(item => new OutboundEmailClickEventListItem
				{
					Id = item.Id,
					LinkType = item.LinkType,
					DestinationUrl = item.DestinationUrl,
					UserAgent = item.UserAgent,
					IpAddressHash = item.IpAddressHash,
					OccurredUtc = item.OccurredUtc
				})
				.ToList()
		};
	}

	public async Task<InitialSetupJobStatusModel> GetInitialSetupJobStatusAsync(CancellationToken cancellationToken = default)
	{
		var state = await _initialSetupStateService.GetStatusAsync(cancellationToken);
		var latestOperation = await _dbContext.BackgroundOperations
			.AsNoTracking()
			.Where(operation => operation.Kind == BackgroundOperationKinds.InitialSetupSeeding)
			.OrderByDescending(operation => operation.Id)
			.FirstOrDefaultAsync(cancellationToken);
		if (latestOperation is null)
		{
			return new InitialSetupJobStatusModel
			{
				IsComplete = state.IsComplete,
				HasActiveOperation = false,
				Status = state.IsComplete ? BackgroundOperationStatuses.Completed : string.Empty,
				Stages = state.IsComplete ? MarkAllComplete(CreateDefaultStageSnapshots()) : CreateDefaultStageSnapshots()
			};
		}

		var metadata = DeserializeMetadata(latestOperation.MetadataJson);
		var stages = DeserializeStageSnapshots(latestOperation.StageStatesJson);
		var liveSnapshot = _initialSeedingProgressService.GetSnapshot();
		if (liveSnapshot.IsRunning || liveSnapshot.IsComplete || !string.IsNullOrWhiteSpace(liveSnapshot.ErrorMessage))
		{
			stages = MergeWithLiveSnapshot(stages, liveSnapshot);
		}

		if (state.IsComplete && stages.All(stage => !stage.IsComplete))
		{
			stages = MarkAllComplete(stages);
		}

		return new InitialSetupJobStatusModel
		{
			HasActiveOperation = latestOperation.Status is BackgroundOperationStatuses.Queued or BackgroundOperationStatuses.Running or BackgroundOperationStatuses.Failed,
			IsComplete = state.IsComplete || latestOperation.Status == BackgroundOperationStatuses.Completed,
			OperationId = latestOperation.Id,
			Status = latestOperation.Status,
			ErrorMessage = latestOperation.ErrorMessage ?? string.Empty,
			DefaultThemeKey = metadata.DefaultThemeKey,
			SelectedThemeKeys = metadata.SelectedThemeKeys,
			Stages = stages
		};
	}

	internal async Task<int> CreateOperationAsync(
		string kind,
		string queueName,
		string summary,
		int? tenantId,
		string? requestedByUserId,
		string? metadataJson,
		string? stageStatesJson,
		CancellationToken cancellationToken)
	{
		var entity = new BackgroundOperation(kind, queueName, summary, tenantId, requestedByUserId, metadataJson, stageStatesJson);
		_dbContext.BackgroundOperations.Add(entity);
		await SaveChangesWithRetryAsync(cancellationToken);
		return entity.Id;
	}

	internal async Task AttachHangfireJobIdAsync(int operationId, string? hangfireJobId, CancellationToken cancellationToken)
	{
		var entity = await _dbContext.BackgroundOperations.FirstOrDefaultAsync(operation => operation.Id == operationId, cancellationToken)
			?? throw new InvalidOperationException("The background operation was not found.");
		entity.AttachHangfireJob(hangfireJobId);
		await SaveChangesWithRetryAsync(cancellationToken);
	}

	internal async Task MarkRunningAsync(int operationId, string? message, CancellationToken cancellationToken)
	{
		var entity = await _dbContext.BackgroundOperations.FirstOrDefaultAsync(operation => operation.Id == operationId, cancellationToken)
			?? throw new InvalidOperationException("The background operation was not found.");
		entity.MarkRunning();
		if (!string.IsNullOrWhiteSpace(message))
		{
			entity.UpdateProgress(entity.CurrentStageKey, entity.CurrentStageLabel, message, entity.ProgressPercent, entity.StageStatesJson);
		}

		await SaveChangesWithRetryAsync(cancellationToken);
	}

	internal async Task ReportInitialSetupProgressAsync(int operationId, InitialSeedingProgressUpdate update, CancellationToken cancellationToken)
	{
		if (!ShouldPersistInitialSetupProgress(update))
		{
			return;
		}

		var entity = await _dbContext.BackgroundOperations
			.Include(operation => operation.Events)
			.FirstOrDefaultAsync(operation => operation.Id == operationId, cancellationToken)
			?? throw new InvalidOperationException("The background operation was not found.");

		var stages = DeserializeStageSnapshots(entity.StageStatesJson);
		var stageFound = false;
		var updatedStages = stages
			.Select(stage =>
			{
				if (!string.Equals(stage.StageKey, update.StageKey, StringComparison.Ordinal))
				{
					return stage;
				}

				stageFound = true;
				return new InitialSeedingStageSnapshot
				{
					StageKey = stage.StageKey,
					StageLabel = string.IsNullOrWhiteSpace(update.StageLabel) ? stage.StageLabel : update.StageLabel,
					ActivityMessage = update.IsComplete ? string.Empty : update.ActivityMessage ?? string.Empty,
					Processed = update.Processed,
					Total = update.Total,
					IsStarted = true,
					IsComplete = update.IsComplete
				};
			})
			.ToList();

		if (!stageFound)
		{
			updatedStages.Add(new InitialSeedingStageSnapshot
			{
				StageKey = update.StageKey,
				StageLabel = string.IsNullOrWhiteSpace(update.StageLabel) ? update.StageKey : update.StageLabel,
				ActivityMessage = update.IsComplete ? string.Empty : update.ActivityMessage ?? string.Empty,
				Processed = update.Processed,
				Total = update.Total,
				IsStarted = true,
				IsComplete = update.IsComplete
			});
		}

		var currentStageLabel = updatedStages.First(item => string.Equals(item.StageKey, update.StageKey, StringComparison.Ordinal)).StageLabel;
		var progressPercent = CalculateOverallProgress(updatedStages);
		var stageStatesJson = SerializeStageSnapshots(updatedStages);
		entity.UpdateProgress(update.StageKey, currentStageLabel, update.ActivityMessage ?? string.Empty, progressPercent, stageStatesJson);

		if (ShouldPersistInitialSetupEvent(update))
		{
			var eventStatus = update.IsComplete ? BackgroundOperationStatuses.Completed : BackgroundOperationStatuses.Running;
			_dbContext.BackgroundOperationEvents.Add(new BackgroundOperationEvent(
				operationId,
				update.StageKey,
				currentStageLabel,
				eventStatus,
				string.IsNullOrWhiteSpace(update.ActivityMessage)
					? $"{currentStageLabel} progress updated."
					: update.ActivityMessage,
				update.Processed,
				update.Total,
				CalculateStagePercent(update.Processed, update.Total, update.IsComplete)));
		}

		await SaveChangesWithRetryAsync(cancellationToken);
	}

	internal async Task CompleteOperationAsync(int operationId, string? message, CancellationToken cancellationToken)
	{
		var entity = await _dbContext.BackgroundOperations.FirstOrDefaultAsync(operation => operation.Id == operationId, cancellationToken)
			?? throw new InvalidOperationException("The background operation was not found.");
		var stages = MarkAllComplete(DeserializeStageSnapshots(entity.StageStatesJson));
		entity.Complete(message, SerializeStageSnapshots(stages));
		await SaveChangesWithRetryAsync(cancellationToken);
	}

	internal async Task FailOperationAsync(int operationId, string? errorMessage, CancellationToken cancellationToken)
	{
		var entity = await _dbContext.BackgroundOperations.FirstOrDefaultAsync(operation => operation.Id == operationId, cancellationToken)
			?? throw new InvalidOperationException("The background operation was not found.");
		entity.Fail(errorMessage, entity.StageStatesJson);
		await SaveChangesWithRetryAsync(cancellationToken);
	}

	internal async Task<BackgroundOperation?> GetLatestInitialSetupOperationEntityAsync(CancellationToken cancellationToken)
	{
		return await _dbContext.BackgroundOperations
			.AsNoTracking()
			.Where(operation => operation.Kind == BackgroundOperationKinds.InitialSetupSeeding)
			.OrderByDescending(operation => operation.Id)
			.FirstOrDefaultAsync(cancellationToken);
	}

	private async Task<BackgroundOperationDetailModel?> GetBackgroundOperationCoreAsync(int id, CancellationToken cancellationToken)
	{
		var entity = await _dbContext.BackgroundOperations
			.AsNoTracking()
			.Include(operation => operation.Events)
			.FirstOrDefaultAsync(operation => operation.Id == id, cancellationToken);
		if (entity is null)
		{
			return null;
		}

		var tenantNames = await LoadTenantNamesAsync(entity.TenantId.HasValue ? [entity.TenantId.Value] : [], cancellationToken);
		var userDisplayNames = await LoadUserDisplayNamesAsync(!string.IsNullOrWhiteSpace(entity.RequestedByUserId) ? [entity.RequestedByUserId!] : [], cancellationToken);

		return new BackgroundOperationDetailModel
		{
			Id = entity.Id,
			Kind = entity.Kind,
			Status = entity.Status,
			Summary = entity.Summary,
			QueueName = entity.QueueName,
			HangfireJobId = entity.HangfireJobId,
			ProgressPercent = entity.ProgressPercent,
			CurrentStageKey = entity.CurrentStageKey,
			CurrentStageLabel = entity.CurrentStageLabel,
			CurrentItemMessage = entity.CurrentItemMessage,
			ErrorMessage = entity.ErrorMessage,
			TenantId = entity.TenantId,
			TenantName = entity.TenantId.HasValue ? tenantNames.GetValueOrDefault(entity.TenantId.Value) ?? string.Empty : string.Empty,
			RequestedByUserId = entity.RequestedByUserId,
			RequestedByDisplayName = !string.IsNullOrWhiteSpace(entity.RequestedByUserId) ? userDisplayNames.GetValueOrDefault(entity.RequestedByUserId) ?? string.Empty : string.Empty,
			CreatedUtc = entity.CreatedUtc,
			StartedUtc = entity.StartedUtc,
			CompletedUtc = entity.CompletedUtc,
			Stages = DeserializeStageSnapshots(entity.StageStatesJson)
				.Select(item => new BackgroundOperationStageSnapshotModel
				{
					StageKey = item.StageKey,
					StageLabel = item.StageLabel,
					ActivityMessage = item.ActivityMessage,
					Processed = item.Processed,
					Total = item.Total,
					IsStarted = item.IsStarted,
					IsComplete = item.IsComplete
				})
				.ToList(),
			Events = entity.Events
				.OrderByDescending(item => item.CreatedUtc)
				.Take(500)
				.Select(item => new BackgroundOperationEventListItem
				{
					Id = item.Id,
					StageKey = item.StageKey,
					StageLabel = item.StageLabel,
					Status = item.Status,
					Message = item.Message,
					Processed = item.Processed,
					Total = item.Total,
					ProgressPercent = item.ProgressPercent,
					CreatedUtc = item.CreatedUtc
				})
				.ToList(),
			LinkedEmails = await _dbContext.OutboundEmails
				.AsNoTracking()
				.Where(email => email.BackgroundOperationId == entity.Id)
				.OrderByDescending(email => email.Id)
				.Select(email => new BackgroundOperationLinkedEmailListItem
				{
					Id = email.Id,
					RecipientEmail = email.RecipientEmail,
					Subject = email.Subject,
					Status = email.Status,
					CreatedUtc = email.CreatedUtc,
					SentUtc = email.SentUtc
				})
				.ToListAsync(cancellationToken)
		};
	}

	internal static InitialSetupOperationMetadata DeserializeMetadata(string? metadataJson)
	{
		if (string.IsNullOrWhiteSpace(metadataJson))
		{
			return new InitialSetupOperationMetadata();
		}

		try
		{
			return JsonSerializer.Deserialize<InitialSetupOperationMetadata>(metadataJson, JsonOptions) ?? new InitialSetupOperationMetadata();
		}
		catch (JsonException)
		{
			return new InitialSetupOperationMetadata();
		}
	}

	internal static List<InitialSeedingStageSnapshot> CreateDefaultStageSnapshots()
	{
		return InitialSeedingStages.Ordered
			.Select(stage => new InitialSeedingStageSnapshot
			{
				StageKey = stage.Key,
				StageLabel = stage.Label
			})
			.ToList();
	}

	private static bool ShouldPersistInitialSetupProgress(InitialSeedingProgressUpdate update)
	{
		if (update.IsComplete || update.Processed <= 1)
		{
			return true;
		}

		if (update.Total > 0 && update.Processed >= update.Total)
		{
			return true;
		}

		return update.Processed % 100 == 0;
	}

	private static bool ShouldPersistInitialSetupEvent(InitialSeedingProgressUpdate update)
	{
		if (update.IsComplete || update.Processed <= 1)
		{
			return true;
		}

		if (update.Total > 0 && update.Processed >= update.Total)
		{
			return true;
		}

		return update.Processed % 500 == 0;
	}

	private static List<InitialSeedingStageSnapshot> MergeWithLiveSnapshot(
		IReadOnlyList<InitialSeedingStageSnapshot> persistedStages,
		InitialSeedingProgressSnapshot liveSnapshot)
	{
		var persistedLookup = persistedStages.ToDictionary(stage => stage.StageKey, StringComparer.Ordinal);

		foreach (var stage in liveSnapshot.Stages)
		{
			if (!stage.IsStarted && !stage.IsComplete && stage.Processed <= 0 && stage.Total <= 0 && string.IsNullOrWhiteSpace(stage.ActivityMessage))
			{
				continue;
			}

			persistedLookup[stage.StageKey] = new InitialSeedingStageSnapshot
			{
				StageKey = stage.StageKey,
				StageLabel = stage.StageLabel,
				ActivityMessage = stage.ActivityMessage,
				Processed = stage.Processed,
				Total = stage.Total,
				IsStarted = stage.IsStarted,
				IsComplete = stage.IsComplete
			};
		}

		return InitialSeedingStages.Ordered
			.Select(stage => persistedLookup.TryGetValue(stage.Key, out var snapshot)
				? snapshot
				: new InitialSeedingStageSnapshot
				{
					StageKey = stage.Key,
					StageLabel = stage.Label
				})
			.ToList();
	}

	private async Task SaveChangesWithRetryAsync(CancellationToken cancellationToken)
	{
		const int maxAttempts = 6;
		DbUpdateException? lastException = null;

		for (var attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				await _dbContext.SaveChangesAsync(cancellationToken);
				return;
			}
			catch (DbUpdateException ex) when (IsSqliteDatabaseLocked(ex) && attempt < maxAttempts)
			{
				lastException = ex;
				await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
			}
			catch (DbUpdateException ex) when (IsSqliteDatabaseLocked(ex))
			{
				lastException = ex;
				break;
			}
		}

		if (lastException is not null)
		{
			throw lastException;
		}

		throw new InvalidOperationException("The background operation changes could not be saved.");
	}

	private static bool IsSqliteDatabaseLocked(DbUpdateException exception)
	{
		return exception.InnerException is SqliteException sqliteException && sqliteException.SqliteErrorCode == 5;
	}

	internal static List<InitialSeedingStageSnapshot> DeserializeStageSnapshots(string? stageStatesJson)
	{
		if (string.IsNullOrWhiteSpace(stageStatesJson))
		{
			return CreateDefaultStageSnapshots();
		}

		try
		{
			var stages = JsonSerializer.Deserialize<List<InitialSeedingStageSnapshot>>(stageStatesJson, JsonOptions);
			if (stages is null || stages.Count == 0)
			{
				return CreateDefaultStageSnapshots();
			}

			var byKey = stages.ToDictionary(stage => stage.StageKey, StringComparer.Ordinal);
			var normalized = new List<InitialSeedingStageSnapshot>();
			foreach (var stage in InitialSeedingStages.Ordered)
			{
				if (byKey.TryGetValue(stage.Key, out var existing))
				{
					normalized.Add(new InitialSeedingStageSnapshot
					{
						StageKey = existing.StageKey,
						StageLabel = string.IsNullOrWhiteSpace(existing.StageLabel) ? stage.Label : existing.StageLabel,
						ActivityMessage = existing.ActivityMessage,
						Processed = existing.Processed,
						Total = existing.Total,
						IsStarted = existing.IsStarted,
						IsComplete = existing.IsComplete
					});
				}
				else
				{
					normalized.Add(new InitialSeedingStageSnapshot
					{
						StageKey = stage.Key,
						StageLabel = stage.Label
					});
				}
			}

			return normalized;
		}
		catch (JsonException)
		{
			return CreateDefaultStageSnapshots();
		}
	}

	internal static string SerializeStageSnapshots(IReadOnlyList<InitialSeedingStageSnapshot> stages)
	{
		return JsonSerializer.Serialize(stages, JsonOptions);
	}

	internal static List<InitialSeedingStageSnapshot> MarkAllComplete(IReadOnlyList<InitialSeedingStageSnapshot> stages)
	{
		return stages.Select(stage => new InitialSeedingStageSnapshot
		{
			StageKey = stage.StageKey,
			StageLabel = stage.StageLabel,
			ActivityMessage = string.Empty,
			Processed = stage.Total > 0 ? stage.Total : Math.Max(stage.Processed, 1),
			Total = stage.Total > 0 ? stage.Total : Math.Max(stage.Processed, 1),
			IsStarted = true,
			IsComplete = true
		}).ToList();
	}

	private static int CalculateOverallProgress(IReadOnlyList<InitialSeedingStageSnapshot> stages)
	{
		if (stages.Count == 0)
		{
			return 0;
		}

		var totalPercent = stages.Sum(stage => CalculateStagePercent(stage.Processed, stage.Total, stage.IsComplete));
		return Math.Clamp((int)Math.Round(totalPercent / (double)stages.Count), 0, 100);
	}

	private static int CalculateStagePercent(int processed, int total, bool isComplete)
	{
		if (isComplete)
		{
			return 100;
		}

		if (total <= 0)
		{
			return 0;
		}

		return Math.Clamp((int)Math.Floor(processed * 100d / total), 0, 100);
	}

	private async Task<Dictionary<int, string>> LoadTenantNamesAsync(int[] tenantIds, CancellationToken cancellationToken)
	{
		if (tenantIds.Length == 0)
		{
			return [];
		}

		return await _dbContext.Tenants
			.AsNoTracking()
			.Where(tenant => tenantIds.Contains(tenant.Id))
			.ToDictionaryAsync(tenant => tenant.Id, tenant => tenant.Name, cancellationToken);
	}

	private async Task<Dictionary<string, string>> LoadUserDisplayNamesAsync(string[] userIds, CancellationToken cancellationToken)
	{
		if (userIds.Length == 0)
		{
			return [];
		}

		var users = await _dbContext.Users
			.AsNoTracking()
			.Where(user => userIds.Contains(user.Id))
			.Select(user => new
			{
				user.Id,
				user.FirstName,
				user.LastName,
				user.Email,
				user.UserName
			})
			.ToListAsync(cancellationToken);

		return users.ToDictionary(
			user => user.Id,
			user => BuildDisplayName(user.FirstName, user.LastName, user.Email, user.UserName),
			StringComparer.Ordinal);
	}

	private static string BuildDisplayName(string? firstName, string? lastName, string? email, string? userName)
	{
		var displayName = string.Join(" ", new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
		return string.IsNullOrWhiteSpace(displayName)
			? email ?? userName ?? "Unknown user"
			: displayName;
	}
}

internal sealed class InitialSetupOperationMetadata
{
	public string DefaultThemeKey { get; set; } = string.Empty;
	public IReadOnlyList<string> SelectedThemeKeys { get; set; } = Array.Empty<string>();
}
