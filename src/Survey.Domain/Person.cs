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
	public int? MailingPostalAddressId { get; private set; }
	public string? MailingAddressLine1 { get; private set; }
	public string? MailingAddressLine2 { get; private set; }
	public string? MailingCity { get; private set; }
	public string? MailingState { get; private set; }
	public string MailingAddress { get; private set; } = string.Empty;
	public string? MailingPostalCode { get; private set; }
	public string PhoneNumber { get; private set; } = string.Empty;
	public string? BestTimeToContact { get; private set; }
	public string? PreferredContactMethod { get; private set; }
	public string Email { get; private set; } = string.Empty;
	public bool IsArchived { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public PostalAddress? PostalAddress { get; private set; }
	public PostalAddress? MailingPostalAddress { get; private set; }
	public ICollection<PersonPhone> Phones { get; } = new List<PersonPhone>();
	public ICollection<PersonEmail> Emails { get; } = new List<PersonEmail>();
	public ICollection<Location> Locations { get; } = new List<Location>();

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
		int? mailingPostalAddressId,
		string mailingAddressLine1,
		string? mailingAddressLine2,
		string mailingCity,
		string mailingState,
		string? mailingPostalCode,
		string? phoneNumber,
		string? bestTimeToContact,
		string? preferredContactMethod,
		string? email,
		string? countryName = null,
		string? mailingCountryName = null)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(firstName, middleName, lastName, postalAddressId, addressLine1, addressLine2, city, state, postalCode, mailingPostalAddressId, mailingAddressLine1, mailingAddressLine2, mailingCity, mailingState, mailingPostalCode, phoneNumber, bestTimeToContact, preferredContactMethod, email, countryName, mailingCountryName);
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
		int? mailingPostalAddressId,
		string mailingAddressLine1,
		string? mailingAddressLine2,
		string mailingCity,
		string mailingState,
		string? mailingPostalCode,
		string? phoneNumber,
		string? bestTimeToContact,
		string? preferredContactMethod,
		string? email,
		string? countryName = null,
		string? mailingCountryName = null)
	{
		FirstName = CleanOptional(firstName, 100) ?? string.Empty;
		MiddleName = CleanOptional(middleName, 100);
		LastName = CleanOptional(lastName, 100) ?? string.Empty;
		PostalAddressId = postalAddressId > 0 ? postalAddressId : null;
		AddressLine1 = RequireValue(addressLine1, nameof(addressLine1), 200);
		AddressLine2 = CleanOptional(addressLine2, 200);
		City = RequireValue(city, nameof(city), 100);
		State = RequireValue(state, nameof(state), 100);
		PostalCode = PostalCodeNormalizer.Normalize(postalCode, nameof(postalCode))
			?? throw new ArgumentException("A value is required.", nameof(postalCode));
		HomeAddress = AddressFormatter.Format(AddressLine1, AddressLine2, City, State, PostalCode, countryName);
		MailingPostalAddressId = mailingPostalAddressId > 0 ? mailingPostalAddressId : null;
		MailingAddressLine1 = RequireValue(mailingAddressLine1, nameof(mailingAddressLine1), 200);
		MailingAddressLine2 = CleanOptional(mailingAddressLine2, 200);
		MailingCity = RequireValue(mailingCity, nameof(mailingCity), 100);
		MailingState = RequireValue(mailingState, nameof(mailingState), 100);
		MailingPostalCode = PostalCodeNormalizer.Normalize(mailingPostalCode, nameof(mailingPostalCode))
			?? throw new ArgumentException("A value is required.", nameof(mailingPostalCode));
		MailingAddress = AddressFormatter.Format(MailingAddressLine1, MailingAddressLine2, MailingCity, MailingState, MailingPostalCode, mailingCountryName);
		UpdatePrimaryContactSnapshot(phoneNumber, email);
		BestTimeToContact = CleanOptional(bestTimeToContact, 100);
		PreferredContactMethod = CleanOptional(preferredContactMethod, 50);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void UpdatePrimaryContactSnapshot(string? phoneNumber, string? email)
	{
		PhoneNumber = CleanOptional(phoneNumber, 50) ?? string.Empty;
		Email = CleanOptional(email, 256) ?? string.Empty;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void SetArchived(bool isArchived)
	{
		IsArchived = isArchived;
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
