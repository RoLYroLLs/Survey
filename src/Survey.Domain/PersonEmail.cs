namespace Survey.Domain;

public class PersonEmail
{
	public int Id { get; private set; }
	public int PersonId { get; private set; }
	public string Label { get; private set; } = string.Empty;
	public string EmailAddress { get; private set; } = string.Empty;
	public int SortOrder { get; private set; }
	public Person Person { get; private set; } = default!;

	private PersonEmail()
	{
	}

	public PersonEmail(int personId, string label, string emailAddress, int sortOrder)
	{
		if (personId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(personId));
		}

		PersonId = personId;
		Update(label, emailAddress, sortOrder);
	}

	public void Update(string label, string emailAddress, int sortOrder)
	{
		Label = RequireValue(label, nameof(label), 50);
		EmailAddress = RequireValue(emailAddress, nameof(emailAddress), 256);
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
