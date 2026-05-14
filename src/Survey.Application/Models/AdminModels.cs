using System.ComponentModel.DataAnnotations;
using Survey.Domain;

namespace Survey.Application.Models;

public class SurveyDefinitionListItem
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int VersionCount { get; set; }
	public bool IsArchived { get; set; }
	public DateTimeOffset UpdatedUtc { get; set; }
}

public class SurveyDefinitionEditModel
{
	public int? Id { get; set; }

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[StringLength(2000)]
	public string? Description { get; set; }

	public bool IsArchived { get; set; }
}

public class SurveyVersionListItem
{
	public int Id { get; set; }
	public int SurveyDefinitionId { get; set; }
	public string SurveyName { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public int VersionNumber { get; set; }
	public bool IsPublished { get; set; }
	public bool IsArchived { get; set; }
	public bool IsLocked { get; set; }
	public int SectionCount { get; set; }
	public int AssignmentCount { get; set; }
}

public class SurveyVersionEditModel
{
	public int? Id { get; set; }

	[Range(1, int.MaxValue)]
	public int SurveyDefinitionId { get; set; }

	[Required]
	[StringLength(200)]
	public string DisplayName { get; set; } = string.Empty;

	[Range(1, int.MaxValue)]
	public int VersionNumber { get; set; } = 1;

	public bool IsPublished { get; set; }
	public bool IsArchived { get; set; }
	public bool IsLocked { get; set; }
	public IReadOnlyList<SelectOption> SurveyOptions { get; set; } = Array.Empty<SelectOption>();
}

public class SurveySectionListItem
{
	public int Id { get; set; }
	public int SurveyDefinitionId { get; set; }
	public int SurveyVersionId { get; set; }
	public string VersionName { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public int SortOrder { get; set; }
	public int QuestionCount { get; set; }
	public bool IsLocked { get; set; }
}

public class SurveySectionEditModel
{
	public int? Id { get; set; }
	public int SurveyDefinitionId { get; set; }

	[Range(1, int.MaxValue)]
	public int SurveyVersionId { get; set; }

	[Required]
	[StringLength(200)]
	public string Title { get; set; } = string.Empty;

	[StringLength(1000)]
	public string? Description { get; set; }

	[Range(0, 9999)]
	public int SortOrder { get; set; }

	public bool IsLocked { get; set; }
	public string VersionName { get; set; } = string.Empty;
	public IReadOnlyList<SelectOption> SurveyVersionOptions { get; set; } = Array.Empty<SelectOption>();
}

public class SurveyQuestionListItem
{
	public int Id { get; set; }
	public int SurveySectionId { get; set; }
	public string SectionTitle { get; set; } = string.Empty;
	public string Prompt { get; set; } = string.Empty;
	public SurveyQuestionType Type { get; set; }
	public bool IsRequired { get; set; }
	public int SortOrder { get; set; }
	public int OptionCount { get; set; }
	public bool IsLocked { get; set; }
}

public class SurveyQuestionEditModel
{
	public int? Id { get; set; }
	public int SurveyDefinitionId { get; set; }
	public int SurveyVersionId { get; set; }

	[Range(1, int.MaxValue)]
	public int SurveySectionId { get; set; }

	[Required]
	[StringLength(2000)]
	public string Prompt { get; set; } = string.Empty;

	[StringLength(1000)]
	public string? HelpText { get; set; }

	[Required]
	public SurveyQuestionType Type { get; set; } = SurveyQuestionType.YesNo;

	public bool IsRequired { get; set; } = true;

	[Range(0, 9999)]
	public int SortOrder { get; set; }

	public bool IsLocked { get; set; }
	public bool SupportsOptions { get; set; }
	public IReadOnlyList<SelectOption> SurveySectionOptions { get; set; } = Array.Empty<SelectOption>();
}

public class QuestionOptionListItem
{
	public int Id { get; set; }
	public int SurveyQuestionId { get; set; }
	public string QuestionPrompt { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;
	public int SortOrder { get; set; }
	public bool IsLocked { get; set; }
}

public class QuestionOptionEditModel
{
	public int? Id { get; set; }
	public int SurveyDefinitionId { get; set; }
	public int SurveyVersionId { get; set; }
	public int SurveySectionId { get; set; }

	[Range(1, int.MaxValue)]
	public int SurveyQuestionId { get; set; }

	[Required]
	[StringLength(200)]
	public string Label { get; set; } = string.Empty;

	[Range(0, 9999)]
	public int SortOrder { get; set; }

	public bool IsLocked { get; set; }
	public string QuestionPrompt { get; set; } = string.Empty;
	public SurveyQuestionType QuestionType { get; set; }
	public bool SupportsOptions { get; set; }
	public IReadOnlyList<SelectOption> SurveyQuestionOptions { get; set; } = Array.Empty<SelectOption>();
}

public class PersonListItem
{
	public int Id { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string? MiddleName { get; set; }
	public string LastName { get; set; } = string.Empty;
	public string? PostalCode { get; set; }
	public string Email { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
}

public class PersonEditModel
{
	public int? Id { get; set; }

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

public class SurveyAssignmentListItem
{
	public int Id { get; set; }
	public string PersonName { get; set; } = string.Empty;
	public string SurveyName { get; set; } = string.Empty;
	public string VersionName { get; set; } = string.Empty;
	public string PublicToken { get; set; } = string.Empty;
	public DateTimeOffset? ExpiresAtUtc { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
	public bool IsCompleted { get; set; }
	public bool IsExpired { get; set; }
}

public class SurveyAssignmentEditModel
{
	public int? Id { get; set; }

	[Range(1, int.MaxValue)]
	public int PersonId { get; set; }

	[Range(1, int.MaxValue)]
	public int SurveyVersionId { get; set; }

	public DateTimeOffset? ExpiresAtUtc { get; set; }
	public string PublicToken { get; set; } = string.Empty;
	public bool IsCompleted { get; set; }
	public IReadOnlyList<SelectOption> PersonOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> SurveyVersionOptions { get; set; } = Array.Empty<SelectOption>();
}

public class SurveyResponseListItem
{
	public int Id { get; set; }
	public int SurveyAssignmentId { get; set; }
	public int PersonId { get; set; }
	public string SurveyName { get; set; } = string.Empty;
	public string VersionName { get; set; } = string.Empty;
	public string RespondentName { get; set; } = string.Empty;
	public string? PostalCode { get; set; }
	public string? CountyName { get; set; }
	public string SubmittedByLabel { get; set; } = string.Empty;
	public DateTimeOffset SubmittedUtc { get; set; }
}

public class SurveyResponseDetailModel
{
	public int Id { get; set; }
	public int SurveyAssignmentId { get; set; }
	public int PersonId { get; set; }
	public int? RespondentPostalAddressId { get; set; }
	public string SurveyName { get; set; } = string.Empty;
	public string VersionName { get; set; } = string.Empty;
	public string RespondentName { get; set; } = string.Empty;
	public string HomeAddress { get; set; } = string.Empty;
	public string? PostalCode { get; set; }
	public string? CountyName { get; set; }
	public string PhoneNumber { get; set; } = string.Empty;
	public string? BestTimeToContact { get; set; }
	public string Email { get; set; } = string.Empty;
	public string SubmittedByLabel { get; set; } = string.Empty;
	public DateTimeOffset SubmittedUtc { get; set; }
	public IReadOnlyList<SurveyResponseAnswerDetailModel> Answers { get; set; } = Array.Empty<SurveyResponseAnswerDetailModel>();
}

public class SurveyResponseAnswerDetailModel
{
	public string Question { get; set; } = string.Empty;
	public string Answer { get; set; } = string.Empty;
}

public class EmployeeListItem
{
	public string Id { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string RoleName { get; set; } = RoleNames.Employee;
}

public class EmployeeEditModel
{
	public string? Id { get; set; }

	[Required]
	[StringLength(100)]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	[StringLength(100)]
	[DataType(DataType.Password)]
	public string? Password { get; set; }

	[StringLength(100)]
	[DataType(DataType.Password)]
	[Compare(nameof(Password))]
	public string? ConfirmPassword { get; set; }

	[Required]
	public string RoleName { get; set; } = RoleNames.Employee;

	public bool IsNew => string.IsNullOrWhiteSpace(Id);
}
