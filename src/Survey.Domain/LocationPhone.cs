namespace Survey.Domain;

public class LocationPhone : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int LocationId { get; private set; }
	public string Label { get; private set; } = string.Empty;
	public string PhoneNumber { get; private set; } = string.Empty;
	public int SortOrder { get; private set; }
	public Location Location { get; private set; } = default!;
	public ICollection<SurveyAssignment> Assignments { get; } = new List<SurveyAssignment>();

	private LocationPhone()
	{
	}

	public LocationPhone(int locationId, string label, string phoneNumber, int sortOrder)
	{
		if (locationId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(locationId));
		}

		LocationId = locationId;
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
