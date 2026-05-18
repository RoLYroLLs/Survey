namespace Survey.Domain;

public class PostalAddress
{
	public int Id { get; private set; }
	public int CountryId { get; private set; }
	public int? StateProvinceId { get; private set; }
	public int? CountyId { get; private set; }
	public string AddressLine1 { get; private set; } = string.Empty;
	public string? AddressLine2 { get; private set; }
	public string City { get; private set; } = string.Empty;
	public string PostalCode { get; private set; } = string.Empty;
	public string FormattedAddress { get; private set; } = string.Empty;
	public string NormalizedKey { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public Country Country { get; private set; } = default!;
	public StateProvince? StateProvince { get; private set; }
	public County? County { get; private set; }
	public ICollection<Person> People { get; } = new List<Person>();
	public ICollection<Person> PersonMailingAddresses { get; } = new List<Person>();
	public ICollection<Location> Locations { get; } = new List<Location>();
	public ICollection<Location> LocationMailingAddresses { get; } = new List<Location>();
	public ICollection<SurveyResponse> SurveyResponses { get; } = new List<SurveyResponse>();
	public ICollection<SurveyResponse> SurveyResponseMailingAddresses { get; } = new List<SurveyResponse>();

	private PostalAddress()
	{
	}

	public PostalAddress(
		int countryId,
		int? stateProvinceId,
		int? countyId,
		string addressLine1,
		string? addressLine2,
		string city,
		string postalCode,
		string countryIso2Code,
		string? stateProvinceCode,
		string countryName)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(countryId, stateProvinceId, countyId, addressLine1, addressLine2, city, postalCode, countryIso2Code, stateProvinceCode, countryName);
	}

	public void Update(
		int countryId,
		int? stateProvinceId,
		int? countyId,
		string addressLine1,
		string? addressLine2,
		string city,
		string postalCode,
		string countryIso2Code,
		string? stateProvinceCode,
		string countryName)
	{
		if (countryId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(countryId));
		}

		CountryId = countryId;
		StateProvinceId = stateProvinceId > 0 ? stateProvinceId : null;
		CountyId = countyId > 0 ? countyId : null;
		AddressLine1 = RequireValue(addressLine1, nameof(addressLine1), 200);
		AddressLine2 = CleanOptional(addressLine2, 200);
		City = RequireValue(city, nameof(city), 100);
		PostalCode = PostalCodeNormalizer.Normalize(postalCode, nameof(postalCode))
			?? throw new ArgumentException("A value is required.", nameof(postalCode));
		FormattedAddress = AddressFormatter.Format(AddressLine1, AddressLine2, City, stateProvinceCode, PostalCode, countryName);
		NormalizedKey = PostalAddressKeyBuilder.Build(countryIso2Code, stateProvinceCode, AddressLine1, AddressLine2, City, PostalCode);
		UpdatedUtc = DateTimeOffset.UtcNow;
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
