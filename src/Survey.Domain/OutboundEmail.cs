namespace Survey.Domain;

public static class OutboundEmailStatuses
{
	public const string Queued = "queued";
	public const string Sending = "sending";
	public const string Sent = "sent";
	public const string Failed = "failed";
}

public static class OutboundEmailSourceTypes
{
	public const string IdentityConfirmation = "identity.confirmation";
	public const string IdentityPasswordReset = "identity.password-reset";
	public const string PlatformInvitation = "platform.invitation";
	public const string TenantInvitation = "tenant.invitation";
	public const string Assignment = "assignment";
}

public class OutboundEmail
{
	public int Id { get; private set; }
	public int? BackgroundOperationId { get; private set; }
	public int? TenantId { get; private set; }
	public string? CreatedByUserId { get; private set; }
	public string TemplateKey { get; private set; } = string.Empty;
	public string SourceType { get; private set; } = string.Empty;
	public string SourceId { get; private set; } = string.Empty;
	public string RecipientName { get; private set; } = string.Empty;
	public string RecipientEmail { get; private set; } = string.Empty;
	public string Subject { get; private set; } = string.Empty;
	public string HtmlBody { get; private set; } = string.Empty;
	public string TextBody { get; private set; } = string.Empty;
	public string TrackingToken { get; private set; } = string.Empty;
	public string Status { get; private set; } = OutboundEmailStatuses.Queued;
	public string? ProviderMessageId { get; private set; }
	public string? LastError { get; private set; }
	public int AttemptCount { get; private set; }
	public DateTimeOffset? FirstOpenedUtc { get; private set; }
	public DateTimeOffset? LastOpenedUtc { get; private set; }
	public int OpenCount { get; private set; }
	public DateTimeOffset? FirstClickedUtc { get; private set; }
	public DateTimeOffset? LastClickedUtc { get; private set; }
	public int ClickCount { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset? SentUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public BackgroundOperation? Operation { get; private set; }
	public ICollection<OutboundEmailAttempt> Attempts { get; } = new List<OutboundEmailAttempt>();
	public ICollection<OutboundEmailClickEvent> ClickEvents { get; } = new List<OutboundEmailClickEvent>();

	private OutboundEmail()
	{
	}

	public OutboundEmail(
		string templateKey,
		string sourceType,
		string sourceId,
		string recipientEmail,
		string subject,
		string htmlBody,
		string textBody,
		string trackingToken,
		int? tenantId = null,
		string? createdByUserId = null,
		string? recipientName = null)
	{
		TemplateKey = RequireValue(templateKey, nameof(templateKey), 100);
		SourceType = RequireValue(sourceType, nameof(sourceType), 100);
		SourceId = RequireValue(sourceId, nameof(sourceId), 100);
		RecipientEmail = RequireValue(recipientEmail, nameof(recipientEmail), 256);
		Subject = RequireValue(subject, nameof(subject), 500);
		HtmlBody = RequireValue(htmlBody, nameof(htmlBody), 32000);
		TextBody = RequireValue(textBody, nameof(textBody), 16000);
		TrackingToken = RequireValue(trackingToken, nameof(trackingToken), 200);
		RecipientName = NormalizeValue(recipientName, 200) ?? string.Empty;
		TenantId = tenantId;
		CreatedByUserId = NormalizeValue(createdByUserId, 450);
		CreatedUtc = DateTimeOffset.UtcNow;
		UpdatedUtc = CreatedUtc;
	}

	public void AttachOperation(int backgroundOperationId)
	{
		if (backgroundOperationId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(backgroundOperationId));
		}

		BackgroundOperationId = backgroundOperationId;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void MarkSending()
	{
		Status = OutboundEmailStatuses.Sending;
		LastError = null;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void IncrementAttempt()
	{
		AttemptCount++;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void MarkSent(string? providerMessageId)
	{
		Status = OutboundEmailStatuses.Sent;
		ProviderMessageId = NormalizeValue(providerMessageId, 200);
		LastError = null;
		SentUtc = DateTimeOffset.UtcNow;
		UpdatedUtc = SentUtc.Value;
	}

	public void MarkFailed(string? errorMessage, string? providerMessageId = null)
	{
		Status = OutboundEmailStatuses.Failed;
		LastError = NormalizeValue(errorMessage, 4000);
		ProviderMessageId = NormalizeValue(providerMessageId, 200);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void RecordOpen()
	{
		var now = DateTimeOffset.UtcNow;
		FirstOpenedUtc ??= now;
		LastOpenedUtc = now;
		OpenCount++;
		UpdatedUtc = now;
	}

	public void RecordClick()
	{
		var now = DateTimeOffset.UtcNow;
		FirstClickedUtc ??= now;
		LastClickedUtc = now;
		ClickCount++;
		UpdatedUtc = now;
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

public class OutboundEmailAttempt
{
	public int Id { get; private set; }
	public int OutboundEmailId { get; private set; }
	public int AttemptNumber { get; private set; }
	public string Status { get; private set; } = string.Empty;
	public string? ProviderMessageId { get; private set; }
	public string? ErrorMessage { get; private set; }
	public DateTimeOffset StartedUtc { get; private set; }
	public DateTimeOffset? CompletedUtc { get; private set; }
	public OutboundEmail Email { get; private set; } = default!;

	private OutboundEmailAttempt()
	{
	}

	public OutboundEmailAttempt(int outboundEmailId, int attemptNumber)
	{
		if (outboundEmailId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(outboundEmailId));
		}

		if (attemptNumber < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(attemptNumber));
		}

		OutboundEmailId = outboundEmailId;
		AttemptNumber = attemptNumber;
		Status = OutboundEmailStatuses.Sending;
		StartedUtc = DateTimeOffset.UtcNow;
	}

	public void MarkSent(string? providerMessageId)
	{
		Status = OutboundEmailStatuses.Sent;
		ProviderMessageId = NormalizeValue(providerMessageId, 200);
		ErrorMessage = null;
		CompletedUtc = DateTimeOffset.UtcNow;
	}

	public void MarkFailed(string? errorMessage, string? providerMessageId = null)
	{
		Status = OutboundEmailStatuses.Failed;
		ErrorMessage = NormalizeValue(errorMessage, 4000);
		ProviderMessageId = NormalizeValue(providerMessageId, 200);
		CompletedUtc = DateTimeOffset.UtcNow;
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

public class OutboundEmailClickEvent
{
	public int Id { get; private set; }
	public int OutboundEmailId { get; private set; }
	public string LinkType { get; private set; } = string.Empty;
	public string DestinationUrl { get; private set; } = string.Empty;
	public string UserAgent { get; private set; } = string.Empty;
	public string IpAddressHash { get; private set; } = string.Empty;
	public DateTimeOffset OccurredUtc { get; private set; }
	public OutboundEmail Email { get; private set; } = default!;

	private OutboundEmailClickEvent()
	{
	}

	public OutboundEmailClickEvent(
		int outboundEmailId,
		string linkType,
		string destinationUrl,
		string? userAgent,
		string? ipAddressHash)
	{
		if (outboundEmailId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(outboundEmailId));
		}

		OutboundEmailId = outboundEmailId;
		LinkType = RequireValue(linkType, nameof(linkType), 100);
		DestinationUrl = RequireValue(destinationUrl, nameof(destinationUrl), 2000);
		UserAgent = NormalizeValue(userAgent, 1000) ?? string.Empty;
		IpAddressHash = NormalizeValue(ipAddressHash, 200) ?? string.Empty;
		OccurredUtc = DateTimeOffset.UtcNow;
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
