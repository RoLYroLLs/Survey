namespace Survey.Domain;

public class Country
{
	public int Id { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string Iso2Code { get; private set; } = string.Empty;
	public string? Iso3Code { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public ICollection<StateProvince> StateProvinces { get; } = new List<StateProvince>();
	public ICollection<PostalAddress> PostalAddresses { get; } = new List<PostalAddress>();

	private Country()
	{
	}

	public Country(string name, string iso2Code, string? iso3Code)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(name, iso2Code, iso3Code);
	}

	public void Update(string name, string iso2Code, string? iso3Code)
	{
		Name = RequireValue(name, nameof(name), 200);
		Iso2Code = RequireValue(iso2Code, nameof(iso2Code), 2).ToUpperInvariant();
		Iso3Code = CleanOptional(iso3Code, 3)?.ToUpperInvariant();
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
