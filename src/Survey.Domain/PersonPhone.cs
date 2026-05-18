namespace Survey.Domain;

public class PersonPhone : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int PersonId { get; private set; }
	public string Label { get; private set; } = string.Empty;
	public string PhoneNumber { get; private set; } = string.Empty;
	public int SortOrder { get; private set; }
	public Person Person { get; private set; } = default!;

	private PersonPhone()
	{
	}

	public PersonPhone(int personId, string label, string phoneNumber, int sortOrder)
	{
		if (personId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(personId));
		}

		PersonId = personId;
		Update(label, phoneNumber, sortOrder);
	}

	public void Update(string label, string phoneNumber, int sortOrder)
	{
		Label = RequireValue(label, nameof(label), 50);
		PhoneNumber = RequireValue(phoneNumber, nameof(phoneNumber), 50);
		SortOrder = Math.Max(0, sortOrder);
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
}
