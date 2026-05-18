using System.ComponentModel.DataAnnotations;

namespace Survey.Application.Models;

public class CountryListItem
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Iso2Code { get; set; } = string.Empty;
	public string? Iso3Code { get; set; }
	public int StateProvinceCount { get; set; }
}

public class CountryEditModel
{
	public int? Id { get; set; }

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(2)]
	public string Iso2Code { get; set; } = string.Empty;

	[StringLength(3)]
	public string? Iso3Code { get; set; }
}

public class StateProvinceListItem
{
	public int Id { get; set; }
	public int CountryId { get; set; }
	public string CountryName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Code { get; set; } = string.Empty;
	public string SubdivisionType { get; set; } = string.Empty;
	public int CountyCount { get; set; }
	public string? CountryFilterName { get; set; }
}

public class StateProvinceEditModel
{
	public int? Id { get; set; }

	[Range(1, int.MaxValue)]
	public int CountryId { get; set; }

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(20)]
	public string Code { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string SubdivisionType { get; set; } = "State";

	public IReadOnlyList<SelectOption> CountryOptions { get; set; } = Array.Empty<SelectOption>();
}

public class CountyListItem
{
	public int Id { get; set; }
	public int StateProvinceId { get; set; }
	public string StateProvinceName { get; set; } = string.Empty;
	public string StateProvinceCode { get; set; } = string.Empty;
	public string CountryName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string FipsCode { get; set; } = string.Empty;
	public int AddressCount { get; set; }
	public int AreaCount { get; set; }
	public string? StateProvinceFilterName { get; set; }
}

public class CountyEditModel
{
	public int? Id { get; set; }

	[Range(1, int.MaxValue)]
	public int StateProvinceId { get; set; }

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(20)]
	public string FipsCode { get; set; } = string.Empty;

	public IReadOnlyList<SelectOption> StateProvinceOptions { get; set; } = Array.Empty<SelectOption>();
}

public class PostalAddressListItem
{
	public int Id { get; set; }
	public int? CountyId { get; set; }
	public string? CountyName { get; set; }
	public string AddressLine1 { get; set; } = string.Empty;
	public string? AddressLine2 { get; set; }
	public string City { get; set; } = string.Empty;
	public string StateProvinceCode { get; set; } = string.Empty;
	public string CountryCode { get; set; } = string.Empty;
	public string PostalCode { get; set; } = string.Empty;
	public string FormattedAddress { get; set; } = string.Empty;
	public int ReferenceCount { get; set; }
}

public class PostalAddressEditModel
{
	public int? Id { get; set; }

	[Range(1, int.MaxValue)]
	public int CountryId { get; set; }

	[Range(1, int.MaxValue)]
	public int StateProvinceId { get; set; }

	public int? CountyId { get; set; }

	[Required]
	[StringLength(200)]
	public string AddressLine1 { get; set; } = string.Empty;

	[StringLength(200)]
	public string? AddressLine2 { get; set; }

	[Required]
	[StringLength(100)]
	public string City { get; set; } = string.Empty;

	[Required]
	[StringLength(20)]
	public string PostalCode { get; set; } = string.Empty;

	public string FormattedAddress { get; set; } = string.Empty;
	public int ReferenceCount { get; set; }
	public IReadOnlyList<SelectOption> CountryOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> StateProvinceOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> CountyOptions { get; set; } = Array.Empty<SelectOption>();
}

public class PostalAddressReferenceViewModel
{
	public int Id { get; set; }
	public string FormattedAddress { get; set; } = string.Empty;
	public string CountryCode { get; set; } = string.Empty;
	public string? CountyName { get; set; }
	public IReadOnlyList<PostalAddressPersonReferenceItem> People { get; set; } = Array.Empty<PostalAddressPersonReferenceItem>();
	public IReadOnlyList<PostalAddressLocationReferenceItem> Locations { get; set; } = Array.Empty<PostalAddressLocationReferenceItem>();
	public IReadOnlyList<PostalAddressResponseReferenceItem> Responses { get; set; } = Array.Empty<PostalAddressResponseReferenceItem>();
	public int ReferenceCount => People.Count + Locations.Count + Responses.Count;
}

public class PostalAddressPersonReferenceItem
{
	public int Id { get; set; }
	public int TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string? Email { get; set; }
	public string? PhoneNumber { get; set; }
}

public class PostalAddressResponseReferenceItem
{
	public int Id { get; set; }
	public int TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public string RespondentName { get; set; } = string.Empty;
	public string SurveyName { get; set; } = string.Empty;
	public string VersionName { get; set; } = string.Empty;
	public DateTimeOffset SubmittedUtc { get; set; }
}

public class PostalAddressLocationReferenceItem
{
	public int Id { get; set; }
	public int TenantId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public int PersonId { get; set; }
	public string PersonName { get; set; } = string.Empty;
	public string Nickname { get; set; } = string.Empty;
	public string? Email { get; set; }
	public string? PhoneNumber { get; set; }
}

public class CountryImportModel
{
	[Required]
	public string CsvContent { get; set; } = string.Empty;

	public bool ReplaceExisting { get; set; }
}

public class StateProvinceImportModel
{
	[Required]
	public string CsvContent { get; set; } = string.Empty;

	public bool ReplaceExisting { get; set; }
}

public class CountyImportModel
{
	[Required]
	public string CsvContent { get; set; } = string.Empty;

	public bool ReplaceExisting { get; set; }
}

public class PostalAddressImportModel
{
	[Required]
	public string CsvContent { get; set; } = string.Empty;

	public bool ReplaceExisting { get; set; }
}

public class GeographyImportResultModel
{
	public int ImportedRowCount { get; set; }
}
