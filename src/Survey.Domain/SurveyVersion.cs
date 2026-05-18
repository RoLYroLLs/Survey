namespace Survey.Domain;

public class SurveyVersion : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int SurveyDefinitionId { get; private set; }
	public string DisplayName { get; private set; } = string.Empty;
	public int VersionNumber { get; private set; }
	public bool IsPublished { get; private set; }
	public bool IsArchived { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public SurveyDefinition SurveyDefinition { get; private set; } = default!;
	public ICollection<SurveySection> Sections { get; } = new List<SurveySection>();
	public ICollection<SurveyAssignment> Assignments { get; } = new List<SurveyAssignment>();

	private SurveyVersion()
	{
	}

	public SurveyVersion(int surveyDefinitionId, string displayName, int versionNumber, bool isPublished)
	{
		SurveyDefinitionId = surveyDefinitionId;
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(displayName, versionNumber, isPublished);
	}

	public void Update(string displayName, int versionNumber, bool isPublished)
	{
		DisplayName = RequireValue(displayName, nameof(displayName), 200);
		VersionNumber = versionNumber < 1 ? throw new ArgumentOutOfRangeException(nameof(versionNumber)) : versionNumber;
		IsPublished = isPublished;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void SetArchived(bool isArchived)
	{
		if (IsArchived == isArchived)
		{
			return;
		}

		IsArchived = isArchived;
		UpdatedUtc = DateTimeOffset.UtcNow;
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
