namespace Survey.Domain;

public class SurveyAssignment : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int LocationId { get; private set; }
	public int? LocationPhoneId { get; private set; }
	public int? LocationEmailId { get; private set; }
	public int SurveyVersionId { get; private set; }
	public string PublicToken { get; private set; } = string.Empty;
	public DateTimeOffset? ExpiresAtUtc { get; private set; }
	public bool IsArchived { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public string? CreatedByUserId { get; private set; }
	public Location Location { get; private set; } = default!;
	public LocationPhone? LocationPhone { get; private set; }
	public LocationEmail? LocationEmail { get; private set; }
	public SurveyVersion SurveyVersion { get; private set; } = default!;
	public SurveyResponse? Response { get; private set; }

	private SurveyAssignment()
	{
	}

	public SurveyAssignment(
		int locationId,
		int? locationPhoneId,
		int? locationEmailId,
		int surveyVersionId,
		string publicToken,
		DateTimeOffset? expiresAtUtc,
		string? createdByUserId)
	{
		if (locationId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(locationId));
		}

		if (!locationPhoneId.HasValue && !locationEmailId.HasValue)
		{
			throw new ArgumentException("At least one contact method is required.");
		}

		LocationId = locationId;
		LocationPhoneId = locationPhoneId > 0 ? locationPhoneId : null;
		LocationEmailId = locationEmailId > 0 ? locationEmailId : null;
		SurveyVersionId = surveyVersionId;
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(publicToken, expiresAtUtc);
		CreatedByUserId = CleanOptional(createdByUserId, 450);
	}

	public void Update(string publicToken, DateTimeOffset? expiresAtUtc)
	{
		PublicToken = RequireValue(publicToken, nameof(publicToken), 100);
		ExpiresAtUtc = expiresAtUtc;
	}

	public void SetArchived(bool isArchived)
	{
		if (IsArchived == isArchived)
		{
			return;
		}

		IsArchived = isArchived;
	}

	public bool IsExpired(DateTimeOffset nowUtc)
	{
		return ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= nowUtc;
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

	private static string? CleanOptional(string? value, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var trimmed = value.Trim();
		return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
	}
}
