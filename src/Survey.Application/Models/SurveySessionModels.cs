using System.ComponentModel.DataAnnotations;
using Survey.Domain;

namespace Survey.Application.Models;

public class SurveySessionModel
{
	public int AssignmentId { get; set; }
	public string Token { get; set; } = string.Empty;
	public string SurveyName { get; set; } = string.Empty;
	public string VersionName { get; set; } = string.Empty;
	public bool IsStaffMode { get; set; }
	public bool IsExpired { get; set; }
	public bool IsCompleted { get; set; }
	public RespondentContactModel Contact { get; set; } = new();
	public IReadOnlyList<SurveySectionStepModel> Sections { get; set; } = Array.Empty<SurveySectionStepModel>();
}

public class RespondentContactModel
{
	[Required]
	[StringLength(100)]
	public string FirstName { get; set; } = string.Empty;

	[StringLength(100)]
	public string? MiddleName { get; set; }

	[Required]
	[StringLength(100)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[StringLength(200)]
	public string AddressLine1 { get; set; } = string.Empty;

	[StringLength(200)]
	public string? AddressLine2 { get; set; }

	[Required]
	[StringLength(100)]
	public string City { get; set; } = string.Empty;

	[Range(1, int.MaxValue)]
	public int CountryId { get; set; }

	[Required]
	[Range(1, int.MaxValue)]
	public int StateProvinceId { get; set; }

	public int? CountyId { get; set; }

	[Required]
	[StringLength(20)]
	public string? PostalCode { get; set; }

	[Required]
	[StringLength(50)]
	public string PhoneNumber { get; set; } = string.Empty;

	[StringLength(100)]
	public string? BestTimeToContact { get; set; }

	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	public IReadOnlyList<SelectOption> CountryOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> StateProvinceOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> CountyOptions { get; set; } = Array.Empty<SelectOption>();
}

public class SurveySectionStepModel
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int SortOrder { get; set; }
	public IReadOnlyList<SurveyQuestionStepModel> Questions { get; set; } = Array.Empty<SurveyQuestionStepModel>();
}

public class SurveyQuestionStepModel
{
	public int Id { get; set; }
	public string Prompt { get; set; } = string.Empty;
	public string? HelpText { get; set; }
	public SurveyQuestionType Type { get; set; }
	public bool IsRequired { get; set; }
	public int SortOrder { get; set; }
	public IReadOnlyList<SelectOption> Options { get; set; } = Array.Empty<SelectOption>();
}

public class SurveySubmissionModel
{
	public int AssignmentId { get; set; }
	public string Token { get; set; } = string.Empty;
	public bool IsStaffMode { get; set; }
	public RespondentContactModel Contact { get; set; } = new();
	public List<SurveyAnswerInputModel> Answers { get; set; } = new();
}

public class SurveyAnswerInputModel
{
	public int QuestionId { get; set; }
	public string? TextAnswer { get; set; }
	public bool? YesNoAnswer { get; set; }
	public int? SelectedOptionId { get; set; }
	public List<int> SelectedOptionIds { get; set; } = new();
}

public class SubmitSurveyResult
{
	public bool Succeeded { get; set; }
	public string Message { get; set; } = string.Empty;
	public int? ResponseId { get; set; }
}
