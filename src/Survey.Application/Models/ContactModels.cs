using System.ComponentModel.DataAnnotations;

namespace Survey.Application.Models;

public static class ContactOptionCatalog
{
	public static readonly IReadOnlyList<SelectOption> PhoneTypeOptions =
	[
		new() { Value = PhoneTypes.Mobile, Label = PhoneTypes.Mobile },
		new() { Value = PhoneTypes.Home, Label = PhoneTypes.Home },
		new() { Value = PhoneTypes.Work, Label = PhoneTypes.Work },
		new() { Value = PhoneTypes.Fax, Label = PhoneTypes.Fax },
		new() { Value = PhoneTypes.Other, Label = PhoneTypes.Other }
	];

	public static readonly IReadOnlyList<SelectOption> EmailTypeOptions =
	[
		new() { Value = EmailTypes.Home, Label = EmailTypes.Home },
		new() { Value = EmailTypes.Work, Label = EmailTypes.Work },
		new() { Value = EmailTypes.Other, Label = EmailTypes.Other }
	];

	public static readonly IReadOnlyList<SelectOption> BestTimeOptions =
	[
		new() { Value = BestTimes.Morning, Label = BestTimes.Morning },
		new() { Value = BestTimes.Afternoon, Label = BestTimes.Afternoon },
		new() { Value = BestTimes.Evening, Label = BestTimes.Evening }
	];

	public static readonly IReadOnlyList<SelectOption> PreferredContactMethodOptions =
	[
		new() { Value = PreferredContactMethods.Call, Label = PreferredContactMethods.Call },
		new() { Value = PreferredContactMethods.Text, Label = PreferredContactMethods.Text },
		new() { Value = PreferredContactMethods.Email, Label = PreferredContactMethods.Email },
		new() { Value = PreferredContactMethods.Mail, Label = PreferredContactMethods.Mail }
	];

	public static class PhoneTypes
	{
		public const string Mobile = "Mobile";
		public const string Home = "Home";
		public const string Work = "Work";
		public const string Fax = "Fax";
		public const string Other = "Other";
	}

	public static class EmailTypes
	{
		public const string Home = "Home";
		public const string Work = "Work";
		public const string Other = "Other";
	}

	public static class BestTimes
	{
		public const string Morning = "Morning";
		public const string Afternoon = "Afternoon";
		public const string Evening = "Evening";
	}

	public static class PreferredContactMethods
	{
		public const string Call = "Call";
		public const string Text = "Text";
		public const string Email = "Email";
		public const string Mail = "Mail";
	}

	public static string NormalizePhoneType(string? value)
	{
		var alias = NormalizePhoneTypeAlias(value);
		return Normalize(alias, PhoneTypes.Home, PhoneTypeOptions);
	}

	public static string NormalizeEmailType(string? value)
	{
		var alias = NormalizeEmailTypeAlias(value);
		return Normalize(alias, EmailTypes.Home, EmailTypeOptions);
	}

	public static string? NormalizeBestTime(string? value)
	{
		var alias = NormalizeBestTimeAlias(value);
		return NormalizeOptional(alias, BestTimeOptions);
	}

	public static string? NormalizePreferredContactMethod(string? value)
	{
		var alias = NormalizePreferredContactMethodAlias(value);
		return NormalizeOptional(alias, PreferredContactMethodOptions);
	}

	private static string Normalize(string? value, string fallback, IReadOnlyList<SelectOption> options)
	{
		return NormalizeOptional(value, options) ?? fallback;
	}

	private static string? NormalizeOptional(string? value, IReadOnlyList<SelectOption> options)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return null;
		}

		var matched = options.FirstOrDefault(option => string.Equals(option.Value, trimmed, StringComparison.OrdinalIgnoreCase));
		return matched?.Value;
	}

	private static string? NormalizePhoneTypeAlias(string? value)
	{
		return value?.Trim().ToUpperInvariant() switch
		{
			"CELL" => PhoneTypes.Mobile,
			"CELLPHONE" => PhoneTypes.Mobile,
			"CELL PHONE" => PhoneTypes.Mobile,
			"PHONE" => PhoneTypes.Home,
			"PRIMARY" => PhoneTypes.Home,
			_ => value
		};
	}

	private static string? NormalizeEmailTypeAlias(string? value)
	{
		return value?.Trim().ToUpperInvariant() switch
		{
			"PRIMARY" => EmailTypes.Home,
			"PERSONAL" => EmailTypes.Home,
			_ => value
		};
	}

	private static string? NormalizeBestTimeAlias(string? value)
	{
		return value?.Trim().ToUpperInvariant() switch
		{
			"MORNINGS" => BestTimes.Morning,
			"AFTERNOONS" => BestTimes.Afternoon,
			"EVENINGS" => BestTimes.Evening,
			_ => value
		};
	}

	private static string? NormalizePreferredContactMethodAlias(string? value)
	{
		return value?.Trim().ToUpperInvariant() switch
		{
			"PHONE" => PreferredContactMethods.Call,
			"CALLING" => PreferredContactMethods.Call,
			"TEXTING" => PreferredContactMethods.Text,
			"SMS" => PreferredContactMethods.Text,
			"E-MAIL" => PreferredContactMethods.Email,
			_ => value
		};
	}
}

public class AddressInputModel
{
	[StringLength(200)]
	public string AddressLine1 { get; set; } = string.Empty;

	[StringLength(200)]
	public string? AddressLine2 { get; set; }

	[StringLength(100)]
	public string City { get; set; } = string.Empty;

	public int CountryId { get; set; }

	public int StateProvinceId { get; set; }

	public int? CountyId { get; set; }

	[StringLength(20)]
	public string? PostalCode { get; set; }

	public IReadOnlyList<SelectOption> CountryOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> StateProvinceOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> CountyOptions { get; set; } = Array.Empty<SelectOption>();
}

public class PhoneContactEditModel
{
	public int? Id { get; set; }

	[Required]
	[StringLength(50)]
	public string Label { get; set; } = ContactOptionCatalog.PhoneTypes.Home;

	[StringLength(50)]
	public string PhoneNumber { get; set; } = string.Empty;

	public bool IsPrimary { get; set; }

	[Range(0, 9999)]
	public int SortOrder { get; set; }
}

public class EmailContactEditModel
{
	public int? Id { get; set; }

	[Required]
	[StringLength(50)]
	public string Label { get; set; } = ContactOptionCatalog.EmailTypes.Home;

	[EmailAddress]
	[StringLength(256)]
	public string EmailAddress { get; set; } = string.Empty;

	public bool IsPrimary { get; set; }

	[Range(0, 9999)]
	public int SortOrder { get; set; }
}
