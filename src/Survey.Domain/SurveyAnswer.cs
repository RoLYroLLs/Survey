namespace Survey.Domain;

public class SurveyAnswer : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int SurveyResponseId { get; private set; }
	public int SurveyQuestionId { get; private set; }
	public string QuestionPromptSnapshot { get; private set; } = string.Empty;
	public SurveyQuestionType QuestionType { get; private set; }
	public string? AnswerText { get; private set; }
	public bool? YesNoValue { get; private set; }
	public int? SelectedOptionId { get; private set; }
	public string? SelectedOptionIdsJson { get; private set; }
	public SurveyResponse SurveyResponse { get; private set; } = default!;
	public SurveyQuestion SurveyQuestion { get; private set; } = default!;

	private SurveyAnswer()
	{
	}

	public SurveyAnswer(
		int surveyQuestionId,
		string questionPromptSnapshot,
		SurveyQuestionType questionType,
		string? answerText,
		bool? yesNoValue,
		int? selectedOptionId,
		string? selectedOptionIdsJson)
	{
		SurveyQuestionId = surveyQuestionId;
		QuestionPromptSnapshot = RequireValue(questionPromptSnapshot, nameof(questionPromptSnapshot), 2000);
		QuestionType = questionType;
		AnswerText = CleanOptional(answerText);
		YesNoValue = yesNoValue;
		SelectedOptionId = selectedOptionId;
		SelectedOptionIdsJson = CleanOptional(selectedOptionIdsJson);
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

	private static string? CleanOptional(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}
}
