using System.ComponentModel.DataAnnotations;

namespace Survey.Application.Models;

public sealed class BackgroundOperationListItem
{
	public int Id { get; set; }
	public string Kind { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string Summary { get; set; } = string.Empty;
	public string QueueName { get; set; } = string.Empty;
	public string? HangfireJobId { get; set; }
	public int ProgressPercent { get; set; }
	public string CurrentStageLabel { get; set; } = string.Empty;
	public string CurrentItemMessage { get; set; } = string.Empty;
	public int? TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public string RequestedByDisplayName { get; set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset? StartedUtc { get; set; }
	public DateTimeOffset? CompletedUtc { get; set; }
}

public sealed class BackgroundOperationEventListItem
{
	public int Id { get; set; }
	public string StageKey { get; set; } = string.Empty;
	public string StageLabel { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public int Processed { get; set; }
	public int Total { get; set; }
	public int ProgressPercent { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
}

public sealed class BackgroundOperationStageSnapshotModel
{
	public string StageKey { get; set; } = string.Empty;
	public string StageLabel { get; set; } = string.Empty;
	public string ActivityMessage { get; set; } = string.Empty;
	public int Processed { get; set; }
	public int Total { get; set; }
	public bool IsStarted { get; set; }
	public bool IsComplete { get; set; }
}

public sealed class BackgroundOperationLinkedEmailListItem
{
	public int Id { get; set; }
	public string RecipientEmail { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset? SentUtc { get; set; }
}

public sealed class BackgroundOperationDetailModel
{
	public int Id { get; set; }
	public string Kind { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string Summary { get; set; } = string.Empty;
	public string QueueName { get; set; } = string.Empty;
	public string? HangfireJobId { get; set; }
	public int ProgressPercent { get; set; }
	public string CurrentStageKey { get; set; } = string.Empty;
	public string CurrentStageLabel { get; set; } = string.Empty;
	public string CurrentItemMessage { get; set; } = string.Empty;
	public string? ErrorMessage { get; set; }
	public int? TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public string? RequestedByUserId { get; set; }
	public string RequestedByDisplayName { get; set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset? StartedUtc { get; set; }
	public DateTimeOffset? CompletedUtc { get; set; }
	public IReadOnlyList<BackgroundOperationStageSnapshotModel> Stages { get; set; } = Array.Empty<BackgroundOperationStageSnapshotModel>();
	public IReadOnlyList<BackgroundOperationEventListItem> Events { get; set; } = Array.Empty<BackgroundOperationEventListItem>();
	public IReadOnlyList<BackgroundOperationLinkedEmailListItem> LinkedEmails { get; set; } = Array.Empty<BackgroundOperationLinkedEmailListItem>();
}

public sealed class QueuedEmailMessage
{
	[Required]
	[StringLength(100)]
	public string TemplateKey { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string SourceType { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string SourceId { get; set; } = string.Empty;

	[StringLength(200)]
	public string? RecipientName { get; set; }

	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string RecipientEmail { get; set; } = string.Empty;

	[Required]
	[StringLength(500)]
	public string Subject { get; set; } = string.Empty;

	[Required]
	public string HtmlBody { get; set; } = string.Empty;

	[Required]
	public string TextBody { get; set; } = string.Empty;

	[Required]
	[StringLength(500)]
	public string BaseUrl { get; set; } = string.Empty;

	public int? TenantId { get; set; }

	[StringLength(450)]
	public string? RequestedByUserId { get; set; }
}

public sealed class QueuedEmailResult
{
	public int OutboundEmailId { get; set; }
	public int BackgroundOperationId { get; set; }
	public string TrackingToken { get; set; } = string.Empty;
}

public sealed class OutboundEmailListItem
{
	public int Id { get; set; }
	public string RecipientEmail { get; set; } = string.Empty;
	public string RecipientName { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string TemplateKey { get; set; } = string.Empty;
	public string SourceType { get; set; } = string.Empty;
	public string SourceId { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string ProviderMessageId { get; set; } = string.Empty;
	public int AttemptCount { get; set; }
	public int OpenCount { get; set; }
	public int ClickCount { get; set; }
	public int? TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset? SentUtc { get; set; }
	public DateTimeOffset? FirstOpenedUtc { get; set; }
	public DateTimeOffset? LastOpenedUtc { get; set; }
	public DateTimeOffset? FirstClickedUtc { get; set; }
	public DateTimeOffset? LastClickedUtc { get; set; }
}

public sealed class OutboundEmailAttemptListItem
{
	public int Id { get; set; }
	public int AttemptNumber { get; set; }
	public string Status { get; set; } = string.Empty;
	public string ProviderMessageId { get; set; } = string.Empty;
	public string ErrorMessage { get; set; } = string.Empty;
	public DateTimeOffset StartedUtc { get; set; }
	public DateTimeOffset? CompletedUtc { get; set; }
}

public sealed class OutboundEmailClickEventListItem
{
	public int Id { get; set; }
	public string LinkType { get; set; } = string.Empty;
	public string DestinationUrl { get; set; } = string.Empty;
	public string UserAgent { get; set; } = string.Empty;
	public string IpAddressHash { get; set; } = string.Empty;
	public DateTimeOffset OccurredUtc { get; set; }
}

public sealed class OutboundEmailDetailModel
{
	public int Id { get; set; }
	public int? BackgroundOperationId { get; set; }
	public string RecipientEmail { get; set; } = string.Empty;
	public string RecipientName { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string HtmlBody { get; set; } = string.Empty;
	public string TextBody { get; set; } = string.Empty;
	public string TemplateKey { get; set; } = string.Empty;
	public string SourceType { get; set; } = string.Empty;
	public string SourceId { get; set; } = string.Empty;
	public string TrackingToken { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string ProviderMessageId { get; set; } = string.Empty;
	public string LastError { get; set; } = string.Empty;
	public int AttemptCount { get; set; }
	public int OpenCount { get; set; }
	public int ClickCount { get; set; }
	public int? TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public string RequestedByDisplayName { get; set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset UpdatedUtc { get; set; }
	public DateTimeOffset? SentUtc { get; set; }
	public DateTimeOffset? FirstOpenedUtc { get; set; }
	public DateTimeOffset? LastOpenedUtc { get; set; }
	public DateTimeOffset? FirstClickedUtc { get; set; }
	public DateTimeOffset? LastClickedUtc { get; set; }
	public IReadOnlyList<OutboundEmailAttemptListItem> Attempts { get; set; } = Array.Empty<OutboundEmailAttemptListItem>();
	public IReadOnlyList<OutboundEmailClickEventListItem> ClickEvents { get; set; } = Array.Empty<OutboundEmailClickEventListItem>();
}

public sealed class InitialSetupJobStatusModel
{
	public bool HasActiveOperation { get; set; }
	public bool IsComplete { get; set; }
	public int? OperationId { get; set; }
	public string Status { get; set; } = string.Empty;
	public string ErrorMessage { get; set; } = string.Empty;
	public string DefaultThemeKey { get; set; } = string.Empty;
	public IReadOnlyList<string> SelectedThemeKeys { get; set; } = Array.Empty<string>();
	public IReadOnlyList<InitialSeedingStageSnapshot> Stages { get; set; } = Array.Empty<InitialSeedingStageSnapshot>();
}

public sealed class InitialSetupJobStartResult
{
	public int OperationId { get; set; }
	public string Status { get; set; } = string.Empty;
	public bool AlreadyRunning { get; set; }
}

public sealed class EmailClickRedirectResult
{
	public bool IsValid { get; set; }
	public string DestinationUrl { get; set; } = string.Empty;
}

public sealed class EmailTransportMessage
{
	public string RecipientEmail { get; set; } = string.Empty;
	public string RecipientName { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string HtmlBody { get; set; } = string.Empty;
	public string TextBody { get; set; } = string.Empty;
}

public sealed class EmailTransportResult
{
	public bool Succeeded { get; set; }
	public string ProviderMessageId { get; set; } = string.Empty;
	public string ErrorMessage { get; set; } = string.Empty;
}
