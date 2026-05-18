namespace Survey.Domain;

public class QuestionOption : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int SurveyQuestionId { get; private set; }
	public string Label { get; private set; } = string.Empty;
	public int SortOrder { get; private set; }
	public SurveyQuestion SurveyQuestion { get; private set; } = default!;

	private QuestionOption()
	{
	}

	public QuestionOption(int surveyQuestionId, string label, int sortOrder)
	{
		SurveyQuestionId = surveyQuestionId;
		Update(label, sortOrder);
	}

	public void Update(string label, int sortOrder)
	{
		var trimmed = label?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			throw new ArgumentException("A value is required.", nameof(label));
		}

		Label = trimmed.Length > 200 ? trimmed[..200] : trimmed;
		SortOrder = sortOrder < 0 ? 0 : sortOrder;
	}
}
