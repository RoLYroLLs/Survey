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

public class RespondentContactModel : IValidatableObject
{
	[Required]
	[StringLength(100)]
	public string FirstName { get; set; } = string.Empty;

	[StringLength(100)]
	public string? MiddleName { get; set; }

	[Required]
	[StringLength(100)]
	public string LastName { get; set; } = string.Empty;

	[StringLength(50)]
	public string PhoneNumber { get; set; } = string.Empty;

	[StringLength(50)]
	public string? PhoneLabel { get; set; }

	[StringLength(100)]
	public string? BestTimeToContact { get; set; }

	[StringLength(50)]
	public string? PreferredContactMethod { get; set; }

	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	[StringLength(50)]
	public string? EmailLabel { get; set; }

	public AddressInputModel PhysicalAddress { get; set; } = new();
	public AddressInputModel MailingAddress { get; set; } = new();
	public AddressInputModel ProfilePhysicalAddress { get; set; } = new();
	public AddressInputModel ProfileMailingAddress { get; set; } = new();

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		foreach (var result in ContactValidationRules.ValidateRequiredAddress(PhysicalAddress, nameof(PhysicalAddress)))
		{
			yield return result;
		}

		foreach (var result in ContactValidationRules.ValidateOptionalAddress(MailingAddress, nameof(MailingAddress)))
		{
			yield return result;
		}

		foreach (var result in ContactValidationRules.ValidateContactMethods(
			[
				new PhoneContactEditModel
				{
					Label = PhoneLabel ?? ContactOptionCatalog.PhoneTypes.Home,
					PhoneNumber = PhoneNumber
				}
			],
			[
				new EmailContactEditModel
				{
					Label = EmailLabel ?? ContactOptionCatalog.EmailTypes.Home,
					EmailAddress = Email
				}
			],
			nameof(PhoneNumber),
			nameof(Email)))
		{
			yield return result;
		}
	}
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
