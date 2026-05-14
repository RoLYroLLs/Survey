namespace Survey.Domain;

public class Person
{
	public int Id { get; private set; }
	public string FirstName { get; private set; } = string.Empty;
	public string? MiddleName { get; private set; }
	public string LastName { get; private set; } = string.Empty;
	public int? PostalAddressId { get; private set; }
	public string? AddressLine1 { get; private set; }
	public string? AddressLine2 { get; private set; }
	public string? City { get; private set; }
	public string? State { get; private set; }
	public string HomeAddress { get; private set; } = string.Empty;
	public string? PostalCode { get; private set; }
	public string PhoneNumber { get; private set; } = string.Empty;
	public string? BestTimeToContact { get; private set; }
	public string Email { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public PostalAddress? PostalAddress { get; private set; }
	public ICollection<SurveyAssignment> Assignments { get; } = new List<SurveyAssignment>();

	private Person()
	{
	}

	public Person(
		string firstName,
		string? middleName,
		string lastName,
		int? postalAddressId,
		string addressLine1,
		string? addressLine2,
		string city,
		string state,
		string? postalCode,
		string phoneNumber,
		string? bestTimeToContact,
		string email,
		string? countryName = null)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(firstName, middleName, lastName, postalAddressId, addressLine1, addressLine2, city, state, postalCode, phoneNumber, bestTimeToContact, email, countryName);
	}

	public void Update(
		string firstName,
		string? middleName,
		string lastName,
		int? postalAddressId,
		string addressLine1,
		string? addressLine2,
		string city,
		string state,
		string? postalCode,
		string phoneNumber,
		string? bestTimeToContact,
		string email,
		string? countryName = null)
	{
		FirstName = RequireValue(firstName, nameof(firstName), 100);
		MiddleName = CleanOptional(middleName, 100);
		LastName = RequireValue(lastName, nameof(lastName), 100);
		PostalAddressId = postalAddressId > 0 ? postalAddressId : null;
		AddressLine1 = RequireValue(addressLine1, nameof(addressLine1), 200);
		AddressLine2 = CleanOptional(addressLine2, 200);
		City = RequireValue(city, nameof(city), 100);
		State = RequireValue(state, nameof(state), 100);
		PostalCode = PostalCodeNormalizer.Normalize(postalCode, nameof(postalCode))
			?? throw new ArgumentException("A value is required.", nameof(postalCode));
		HomeAddress = AddressFormatter.Format(AddressLine1, AddressLine2, City, State, PostalCode, countryName);
		PhoneNumber = RequireValue(phoneNumber, nameof(phoneNumber), 50);
		BestTimeToContact = CleanOptional(bestTimeToContact, 100);
		Email = RequireValue(email, nameof(email), 256);
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
