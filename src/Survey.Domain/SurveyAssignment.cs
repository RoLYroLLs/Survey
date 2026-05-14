namespace Survey.Domain;

public class SurveyAssignment
{
	public int Id { get; private set; }
	public int PersonId { get; private set; }
	public int SurveyVersionId { get; private set; }
	public string PublicToken { get; private set; } = string.Empty;
	public DateTimeOffset? ExpiresAtUtc { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public string? CreatedByUserId { get; private set; }
	public Person Person { get; private set; } = default!;
	public SurveyVersion SurveyVersion { get; private set; } = default!;
	public SurveyResponse? Response { get; private set; }

	private SurveyAssignment()
	{
	}

	public SurveyAssignment(
		int personId,
		int surveyVersionId,
		string publicToken,
		DateTimeOffset? expiresAtUtc,
		string? createdByUserId)
	{
		PersonId = personId;
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
