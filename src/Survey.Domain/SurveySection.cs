namespace Survey.Domain;

public class SurveySection : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int SurveyVersionId { get; private set; }
	public string Title { get; private set; } = string.Empty;
	public string? Description { get; private set; }
	public int SortOrder { get; private set; }
	public SurveyVersion SurveyVersion { get; private set; } = default!;
	public ICollection<SurveyQuestion> Questions { get; } = new List<SurveyQuestion>();

	private SurveySection()
	{
	}

	public SurveySection(int surveyVersionId, string title, string? description, int sortOrder)
	{
		SurveyVersionId = surveyVersionId;
		Update(title, description, sortOrder);
	}

	public void Update(string title, string? description, int sortOrder)
	{
		Title = RequireValue(title, nameof(title), 200);
		Description = CleanOptional(description, 1000);
		SortOrder = sortOrder < 0 ? 0 : sortOrder;
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
