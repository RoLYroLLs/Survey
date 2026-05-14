namespace Survey.Domain;

public class SurveyQuestion
{
	public int Id { get; private set; }
	public int SurveySectionId { get; private set; }
	public string Prompt { get; private set; } = string.Empty;
	public string? HelpText { get; private set; }
	public SurveyQuestionType Type { get; private set; }
	public bool IsRequired { get; private set; }
	public int SortOrder { get; private set; }
	public SurveySection SurveySection { get; private set; } = default!;
	public ICollection<QuestionOption> Options { get; } = new List<QuestionOption>();

	private SurveyQuestion()
	{
	}

	public SurveyQuestion(
		int surveySectionId,
		string prompt,
		string? helpText,
		SurveyQuestionType type,
		bool isRequired,
		int sortOrder)
	{
		SurveySectionId = surveySectionId;
		Update(prompt, helpText, type, isRequired, sortOrder);
	}

	public void Update(
		string prompt,
		string? helpText,
		SurveyQuestionType type,
		bool isRequired,
		int sortOrder)
	{
		Prompt = RequireValue(prompt, nameof(prompt), 2000);
		HelpText = CleanOptional(helpText, 1000);
		Type = type;
		IsRequired = isRequired;
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
