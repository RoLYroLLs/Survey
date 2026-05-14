namespace Survey.Domain;

public class SurveyResponse
{
	public int Id { get; private set; }
	public int SurveyAssignmentId { get; private set; }
	public string? SubmittedByUserId { get; private set; }
	public bool SubmittedByEmployee { get; private set; }
	public DateTimeOffset SubmittedUtc { get; private set; }
	public string RespondentFirstName { get; private set; } = string.Empty;
	public string? RespondentMiddleName { get; private set; }
	public string RespondentLastName { get; private set; } = string.Empty;
	public int? RespondentPostalAddressId { get; private set; }
	public string? RespondentAddressLine1 { get; private set; }
	public string? RespondentAddressLine2 { get; private set; }
	public string? RespondentCity { get; private set; }
	public string? RespondentState { get; private set; }
	public string RespondentHomeAddress { get; private set; } = string.Empty;
	public string? RespondentPostalCode { get; private set; }
	public string? RespondentCountyFipsSnapshot { get; private set; }
	public string? RespondentCountyNameSnapshot { get; private set; }
	public string? RespondentStateCodeSnapshot { get; private set; }
	public string RespondentPhoneNumber { get; private set; } = string.Empty;
	public string? RespondentBestTimeToContact { get; private set; }
	public string RespondentEmail { get; private set; } = string.Empty;
	public string SurveyNameSnapshot { get; private set; } = string.Empty;
	public string SurveyVersionNameSnapshot { get; private set; } = string.Empty;
	public PostalAddress? RespondentPostalAddress { get; private set; }
	public SurveyAssignment SurveyAssignment { get; private set; } = default!;
	public ICollection<SurveyAnswer> Answers { get; } = new List<SurveyAnswer>();

	private SurveyResponse()
	{
	}

	public SurveyResponse(
		int surveyAssignmentId,
		string? submittedByUserId,
		bool submittedByEmployee,
		string respondentFirstName,
		string? respondentMiddleName,
		string respondentLastName,
		int? respondentPostalAddressId,
		string respondentAddressLine1,
		string? respondentAddressLine2,
		string respondentCity,
		string respondentState,
		string? respondentPostalCode,
		string? respondentCountyFipsSnapshot,
		string? respondentCountyNameSnapshot,
		string? respondentStateCodeSnapshot,
		string respondentPhoneNumber,
		string? respondentBestTimeToContact,
		string respondentEmail,
		string surveyNameSnapshot,
		string surveyVersionNameSnapshot,
		string? countryName = null)
	{
		SurveyAssignmentId = surveyAssignmentId;
		SubmittedByUserId = CleanOptional(submittedByUserId, 450);
		SubmittedByEmployee = submittedByEmployee;
		SubmittedUtc = DateTimeOffset.UtcNow;
		RespondentFirstName = RequireValue(respondentFirstName, nameof(respondentFirstName), 100);
		RespondentMiddleName = CleanOptional(respondentMiddleName, 100);
		RespondentLastName = RequireValue(respondentLastName, nameof(respondentLastName), 100);
		RespondentPostalAddressId = respondentPostalAddressId > 0 ? respondentPostalAddressId : null;
		RespondentAddressLine1 = RequireValue(respondentAddressLine1, nameof(respondentAddressLine1), 200);
		RespondentAddressLine2 = CleanOptional(respondentAddressLine2, 200);
		RespondentCity = RequireValue(respondentCity, nameof(respondentCity), 100);
		RespondentState = RequireValue(respondentState, nameof(respondentState), 100);
		RespondentPostalCode = PostalCodeNormalizer.Normalize(respondentPostalCode, nameof(respondentPostalCode))
			?? throw new ArgumentException("A value is required.", nameof(respondentPostalCode));
		RespondentHomeAddress = AddressFormatter.Format(RespondentAddressLine1, RespondentAddressLine2, RespondentCity, RespondentState, RespondentPostalCode, countryName);
		RespondentCountyFipsSnapshot = CleanOptional(respondentCountyFipsSnapshot, 5);
		RespondentCountyNameSnapshot = CleanOptional(respondentCountyNameSnapshot, 200);
		RespondentStateCodeSnapshot = CleanOptional(respondentStateCodeSnapshot, 2)?.ToUpperInvariant();
		RespondentPhoneNumber = RequireValue(respondentPhoneNumber, nameof(respondentPhoneNumber), 50);
		RespondentBestTimeToContact = CleanOptional(respondentBestTimeToContact, 100);
		RespondentEmail = RequireValue(respondentEmail, nameof(respondentEmail), 256);
		SurveyNameSnapshot = RequireValue(surveyNameSnapshot, nameof(surveyNameSnapshot), 200);
		SurveyVersionNameSnapshot = RequireValue(surveyVersionNameSnapshot, nameof(surveyVersionNameSnapshot), 200);
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
