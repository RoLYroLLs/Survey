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

	[Range(1, 9999)]
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

	[Range(1, 9999)]
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

	[Range(1, 9999)]
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
	public int LocationCount { get; set; }
	public bool IsArchived { get; set; }
}

public class PersonEditModel : IValidatableObject
{
	public int? Id { get; set; }

	[StringLength(100)]
	public string FirstName { get; set; } = string.Empty;

	[StringLength(100)]
	public string? MiddleName { get; set; }

	[StringLength(100)]
	public string LastName { get; set; } = string.Empty;

	[StringLength(100)]
	public string? BestTimeToContact { get; set; }

	[StringLength(50)]
	public string? PreferredContactMethod { get; set; }

	public AddressInputModel PhysicalAddress { get; set; } = new();
	public AddressInputModel MailingAddress { get; set; } = new();
	public List<PhoneContactEditModel> Phones { get; set; } = [];
	public List<EmailContactEditModel> Emails { get; set; } = [];
	public IReadOnlyList<LocationListItem> Locations { get; set; } = Array.Empty<LocationListItem>();
	public string ContactMethodsValidationMessage { get; set; } = string.Empty;
	public bool IsArchived { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		foreach (var result in ContactValidationRules.ValidateRequiredAddress(PhysicalAddress, nameof(PhysicalAddress), "Address"))
		{
			yield return result;
		}

		foreach (var result in ContactValidationRules.ValidateRequiredAddress(MailingAddress, nameof(MailingAddress), "Mailing Address"))
		{
			yield return result;
		}

		foreach (var result in ContactValidationRules.ValidateContactMethods(Phones, Emails, nameof(ContactMethodsValidationMessage)))
		{
			yield return result;
		}
	}
}

public class LocationListItem
{
	public int Id { get; set; }
	public int PersonId { get; set; }
	public string PersonName { get; set; } = string.Empty;
	public string Nickname { get; set; } = string.Empty;
	public string? PostalCode { get; set; }
	public string Email { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public int AssignmentCount { get; set; }
}

public class LocationEditModel : IValidatableObject
{
	public int? Id { get; set; }

	[Range(1, int.MaxValue)]
	public int PersonId { get; set; }

	public string PersonName { get; set; } = string.Empty;

	[Required]
	[StringLength(200)]
	public string Nickname { get; set; } = string.Empty;

	public AddressInputModel PhysicalAddress { get; set; } = new();
	public AddressInputModel MailingAddress { get; set; } = new();
	public AddressInputModel ProfilePhysicalAddress { get; set; } = new();
	public AddressInputModel ProfileMailingAddress { get; set; } = new();
	public List<PhoneContactEditModel> Phones { get; set; } = [];
	public List<EmailContactEditModel> Emails { get; set; } = [];
	public IReadOnlyList<PhoneContactEditModel> ProfilePhones { get; set; } = Array.Empty<PhoneContactEditModel>();
	public IReadOnlyList<EmailContactEditModel> ProfileEmails { get; set; } = Array.Empty<EmailContactEditModel>();
	public IReadOnlyList<SelectOption> PersonOptions { get; set; } = Array.Empty<SelectOption>();
	public string ContactMethodsValidationMessage { get; set; } = string.Empty;

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		foreach (var result in ContactValidationRules.ValidateRequiredAddress(PhysicalAddress, nameof(PhysicalAddress), "Address"))
		{
			yield return result;
		}

		foreach (var result in ContactValidationRules.ValidateOptionalAddress(MailingAddress, nameof(MailingAddress), "Mailing Address"))
		{
			yield return result;
		}

		foreach (var result in ContactValidationRules.ValidateContactMethods(Phones, Emails, nameof(ContactMethodsValidationMessage)))
		{
			yield return result;
		}
	}
}

internal static class ContactValidationRules
{
	private static readonly EmailAddressAttribute EmailValidator = new();

	public static IEnumerable<ValidationResult> ValidateRequiredAddress(AddressInputModel address, string prefix, string addressLabel)
	{
		if (address.CountryId <= 0)
		{
			yield return new ValidationResult($"{addressLabel}: Country is required.", [$"{prefix}.{nameof(AddressInputModel.CountryId)}"]);
		}

		if (address.StateProvinceId <= 0)
		{
			yield return new ValidationResult($"{addressLabel}: State / Territory is required.", [$"{prefix}.{nameof(AddressInputModel.StateProvinceId)}"]);
		}

		if (!address.CountyId.HasValue || address.CountyId.Value <= 0)
		{
			yield return new ValidationResult($"{addressLabel}: County is required.", [$"{prefix}.{nameof(AddressInputModel.CountyId)}"]);
		}

		if (string.IsNullOrWhiteSpace(address.AddressLine1))
		{
			yield return new ValidationResult($"{addressLabel}: Address Line 1 is required.", [$"{prefix}.{nameof(AddressInputModel.AddressLine1)}"]);
		}

		if (string.IsNullOrWhiteSpace(address.City))
		{
			yield return new ValidationResult($"{addressLabel}: City is required.", [$"{prefix}.{nameof(AddressInputModel.City)}"]);
		}

		if (string.IsNullOrWhiteSpace(address.PostalCode))
		{
			yield return new ValidationResult($"{addressLabel}: Postal Code is required.", [$"{prefix}.{nameof(AddressInputModel.PostalCode)}"]);
		}
	}

	public static IEnumerable<ValidationResult> ValidateOptionalAddress(AddressInputModel address, string prefix, string addressLabel)
	{
		if (IsAddressBlank(address))
		{
			yield break;
		}

		foreach (var result in ValidateRequiredAddress(address, prefix, addressLabel))
		{
			yield return result;
		}
	}

	public static IEnumerable<ValidationResult> ValidateContactMethods(
		IReadOnlyList<PhoneContactEditModel> phones,
		IReadOnlyList<EmailContactEditModel> emails,
		string validationMemberName)
	{
		var hasPhone = phones.Any(phone => !string.IsNullOrWhiteSpace(phone.PhoneNumber));
		var hasEmail = emails.Any(email => !string.IsNullOrWhiteSpace(email.EmailAddress));

		if (!hasPhone && !hasEmail)
		{
			yield return new ValidationResult("Enter at least one phone number or email address.", [validationMemberName]);
		}

		foreach (var email in emails.Where(item => !string.IsNullOrWhiteSpace(item.EmailAddress)))
		{
			if (!EmailValidator.IsValid(email.EmailAddress))
			{
				yield return new ValidationResult("Enter a valid email address.", [validationMemberName]);
				yield break;
			}
		}
	}

	private static bool IsAddressBlank(AddressInputModel address)
	{
		return address.StateProvinceId <= 0
			&& !address.CountyId.HasValue
			&& string.IsNullOrWhiteSpace(address.AddressLine1)
			&& string.IsNullOrWhiteSpace(address.AddressLine2)
			&& string.IsNullOrWhiteSpace(address.City)
			&& string.IsNullOrWhiteSpace(address.PostalCode);
	}
}

public class SurveyAssignmentListItem
{
	public int Id { get; set; }
	public string PersonName { get; set; } = string.Empty;
	public string LocationName { get; set; } = string.Empty;
	public string SurveyName { get; set; } = string.Empty;
	public string VersionName { get; set; } = string.Empty;
	public string PublicToken { get; set; } = string.Empty;
	public DateTimeOffset? ExpiresAtUtc { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
	public bool IsArchived { get; set; }
	public bool IsCompleted { get; set; }
	public bool IsExpired { get; set; }
	public int StatusSortOrder { get; set; }
	public bool IsFillable => !IsArchived && !IsCompleted && !IsExpired;
	public bool IsEditable => !IsCompleted;
}

public class SurveyAssignmentEditModel : IValidatableObject
{
	public int? Id { get; set; }

	[Range(1, int.MaxValue)]
	public int PersonId { get; set; }

	[Range(1, int.MaxValue)]
	public int LocationId { get; set; }

	public int? LocationPhoneId { get; set; }

	public int? LocationEmailId { get; set; }

	[Range(1, int.MaxValue)]
	public int SurveyVersionId { get; set; }

	public DateTimeOffset? ExpiresAtUtc { get; set; }
	public string PublicToken { get; set; } = string.Empty;
	public bool IsArchived { get; set; }
	public bool IsCompleted { get; set; }
	public bool IsExpired { get; set; }
	public bool IsFillable => !IsArchived && !IsCompleted && !IsExpired;
	public bool IsEditable => !IsCompleted;
	public IReadOnlyList<SelectOption> LocationOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> LocationPhoneOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> LocationEmailOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> PersonOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> SurveyVersionOptions { get; set; } = Array.Empty<SelectOption>();

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (!LocationPhoneId.HasValue && !LocationEmailId.HasValue)
		{
			yield return new ValidationResult("Select at least one location phone or location email.", [nameof(LocationPhoneId), nameof(LocationEmailId)]);
		}
	}
}

public class SurveyResponseListItem
{
	public int Id { get; set; }
	public int SurveyAssignmentId { get; set; }
	public int PersonId { get; set; }
	public int LocationId { get; set; }
	public string PersonName { get; set; } = string.Empty;
	public string LocationName { get; set; } = string.Empty;
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
	public int LocationId { get; set; }
	public int? RespondentPostalAddressId { get; set; }
	public int? RespondentMailingPostalAddressId { get; set; }
	public string PersonName { get; set; } = string.Empty;
	public string LocationName { get; set; } = string.Empty;
	public string SurveyName { get; set; } = string.Empty;
	public string VersionName { get; set; } = string.Empty;
	public string RespondentName { get; set; } = string.Empty;
	public string HomeAddress { get; set; } = string.Empty;
	public string MailingAddress { get; set; } = string.Empty;
	public string? PostalCode { get; set; }
	public string? CountyName { get; set; }
	public string PhoneNumber { get; set; } = string.Empty;
	public string? PhoneLabel { get; set; }
	public string? BestTimeToContact { get; set; }
	public string? PreferredContactMethod { get; set; }
	public string Email { get; set; } = string.Empty;
	public string? EmailLabel { get; set; }
	public string SubmittedByLabel { get; set; } = string.Empty;
	public DateTimeOffset SubmittedUtc { get; set; }
	public IReadOnlyList<SurveyResponseAnswerDetailModel> Answers { get; set; } = Array.Empty<SurveyResponseAnswerDetailModel>();
}

public class SurveyResponseAnswerDetailModel
{
	public string Question { get; set; } = string.Empty;
	public string Answer { get; set; } = string.Empty;
}
