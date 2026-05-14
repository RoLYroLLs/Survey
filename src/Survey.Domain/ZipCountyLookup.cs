namespace Survey.Domain;

public class ZipCountyLookup
{
	public int Id { get; private set; }
	public string ZipCode { get; private set; } = string.Empty;
	public string CountyFips { get; private set; } = string.Empty;
	public string CountyName { get; private set; } = string.Empty;
	public string StateCode { get; private set; } = string.Empty;
	public decimal ResidentialRatio { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }

	private ZipCountyLookup()
	{
	}

	public ZipCountyLookup(
		string zipCode,
		string countyFips,
		string countyName,
		string stateCode,
		decimal residentialRatio)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(zipCode, countyFips, countyName, stateCode, residentialRatio);
	}

	public void Update(
		string zipCode,
		string countyFips,
		string countyName,
		string stateCode,
		decimal residentialRatio)
	{
		if (residentialRatio < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(residentialRatio));
		}

		ZipCode = PostalCodeNormalizer.Normalize(zipCode, nameof(zipCode))
			?? throw new ArgumentException("A value is required.", nameof(zipCode));
		CountyFips = RequireValue(countyFips, nameof(countyFips), 5);
		CountyName = RequireValue(countyName, nameof(countyName), 200);
		StateCode = RequireValue(stateCode, nameof(stateCode), 2).ToUpperInvariant();
		ResidentialRatio = residentialRatio;
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
}
