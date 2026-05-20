namespace Survey.Domain;

public static class BackgroundOperationKinds
{
	public const string InitialSetupSeeding = "initial-setup.seeding";
	public const string OutboundEmailDispatch = "outbound-email.dispatch";

	public static readonly IReadOnlyList<string> All =
	[
		InitialSetupSeeding,
		OutboundEmailDispatch
	];
}

public static class BackgroundOperationStatuses
{
	public const string Queued = "queued";
	public const string Running = "running";
	public const string Completed = "completed";
	public const string Failed = "failed";
}

public class BackgroundOperation
{
	public int Id { get; private set; }
	public string Kind { get; private set; } = string.Empty;
	public string Status { get; private set; } = BackgroundOperationStatuses.Queued;
	public string QueueName { get; private set; } = string.Empty;
	public string Summary { get; private set; } = string.Empty;
	public int? TenantId { get; private set; }
	public string? RequestedByUserId { get; private set; }
	public string? HangfireJobId { get; private set; }
	public int ProgressPercent { get; private set; }
	public string CurrentStageKey { get; private set; } = string.Empty;
	public string CurrentStageLabel { get; private set; } = string.Empty;
	public string CurrentItemMessage { get; private set; } = string.Empty;
	public string StageStatesJson { get; private set; } = string.Empty;
	public string MetadataJson { get; private set; } = string.Empty;
	public string? ErrorMessage { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset? StartedUtc { get; private set; }
	public DateTimeOffset? CompletedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public ICollection<BackgroundOperationEvent> Events { get; } = new List<BackgroundOperationEvent>();

	private BackgroundOperation()
	{
	}

	public BackgroundOperation(
		string kind,
		string queueName,
		string summary,
		int? tenantId = null,
		string? requestedByUserId = null,
		string? metadataJson = null,
		string? stageStatesJson = null)
	{
		Kind = RequireValue(kind, nameof(kind), 100);
		QueueName = RequireValue(queueName, nameof(queueName), 100);
		Summary = RequireValue(summary, nameof(summary), 500);
		TenantId = tenantId;
		RequestedByUserId = NormalizeValue(requestedByUserId, 450);
		MetadataJson = NormalizeJson(metadataJson);
		StageStatesJson = NormalizeJson(stageStatesJson);
		CreatedUtc = DateTimeOffset.UtcNow;
		UpdatedUtc = CreatedUtc;
	}

	public void AttachHangfireJob(string? hangfireJobId)
	{
		HangfireJobId = NormalizeValue(hangfireJobId, 100);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void MarkRunning()
	{
		Status = BackgroundOperationStatuses.Running;
		StartedUtc ??= DateTimeOffset.UtcNow;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void UpdateProgress(
		string currentStageKey,
		string currentStageLabel,
		string currentItemMessage,
		int progressPercent,
		string? stageStatesJson)
	{
		Status = BackgroundOperationStatuses.Running;
		StartedUtc ??= DateTimeOffset.UtcNow;
		CurrentStageKey = NormalizeValue(currentStageKey, 100) ?? string.Empty;
		CurrentStageLabel = NormalizeValue(currentStageLabel, 200) ?? string.Empty;
		CurrentItemMessage = NormalizeValue(currentItemMessage, 1000) ?? string.Empty;
		ProgressPercent = Math.Clamp(progressPercent, 0, 100);
		StageStatesJson = NormalizeJson(stageStatesJson);
		ErrorMessage = null;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void UpdateMetadata(string? metadataJson)
	{
		MetadataJson = NormalizeJson(metadataJson);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void Complete(string? currentItemMessage, string? stageStatesJson)
	{
		Status = BackgroundOperationStatuses.Completed;
		ProgressPercent = 100;
		CurrentItemMessage = NormalizeValue(currentItemMessage, 1000) ?? string.Empty;
		StageStatesJson = NormalizeJson(stageStatesJson);
		ErrorMessage = null;
		StartedUtc ??= DateTimeOffset.UtcNow;
		CompletedUtc = DateTimeOffset.UtcNow;
		UpdatedUtc = CompletedUtc.Value;
	}

	public void Fail(string? errorMessage, string? stageStatesJson)
	{
		Status = BackgroundOperationStatuses.Failed;
		ErrorMessage = NormalizeValue(errorMessage, 4000);
		StageStatesJson = NormalizeJson(stageStatesJson);
		CompletedUtc = DateTimeOffset.UtcNow;
		UpdatedUtc = CompletedUtc.Value;
	}

	private static string RequireValue(string? value, string paramName, int maxLength)
	{
		var normalized = NormalizeValue(value, maxLength);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			throw new ArgumentException("A value is required.", paramName);
		}

		return normalized;
	}

	private static string NormalizeJson(string? value)
	{
		return NormalizeValue(value, 32000) ?? string.Empty;
	}

	private static string? NormalizeValue(string? value, int maxLength)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return null;
		}

		return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
	}
}

public class BackgroundOperationEvent
{
	public int Id { get; private set; }
	public int BackgroundOperationId { get; private set; }
	public string StageKey { get; private set; } = string.Empty;
	public string StageLabel { get; private set; } = string.Empty;
	public string Status { get; private set; } = string.Empty;
	public string Message { get; private set; } = string.Empty;
	public int Processed { get; private set; }
	public int Total { get; private set; }
	public int ProgressPercent { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public BackgroundOperation Operation { get; private set; } = default!;

	private BackgroundOperationEvent()
	{
	}

	public BackgroundOperationEvent(
		int backgroundOperationId,
		string stageKey,
		string stageLabel,
		string status,
		string message,
		int processed,
		int total,
		int progressPercent)
	{
		if (backgroundOperationId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(backgroundOperationId));
		}

		BackgroundOperationId = backgroundOperationId;
		StageKey = RequireValue(stageKey, nameof(stageKey), 100);
		StageLabel = RequireValue(stageLabel, nameof(stageLabel), 200);
		Status = RequireValue(status, nameof(status), 50);
		Message = RequireValue(message, nameof(message), 2000);
		Processed = processed;
		Total = total;
		ProgressPercent = Math.Clamp(progressPercent, 0, 100);
		CreatedUtc = DateTimeOffset.UtcNow;
	}

	private static string RequireValue(string? value, string paramName, int maxLength)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			throw new ArgumentException("A value is required.", paramName);
		}

		return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
	}
}
