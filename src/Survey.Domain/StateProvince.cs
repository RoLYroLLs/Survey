namespace Survey.Domain;

public class StateProvince
{
	public int Id { get; private set; }
	public int CountryId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string Code { get; private set; } = string.Empty;
	public string SubdivisionType { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public Country Country { get; private set; } = default!;
	public ICollection<County> Counties { get; } = new List<County>();
	public ICollection<PostalAddress> PostalAddresses { get; } = new List<PostalAddress>();
	public ICollection<TenantVisibleStateProvince> TenantVisibility { get; } = new List<TenantVisibleStateProvince>();

	private StateProvince()
	{
	}

	public StateProvince(int countryId, string name, string code, string subdivisionType)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(countryId, name, code, subdivisionType);
	}

	public void Update(int countryId, string name, string code, string subdivisionType)
	{
		if (countryId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(countryId));
		}

		CountryId = countryId;
		Name = RequireValue(name, nameof(name), 200);
		Code = RequireValue(code, nameof(code), 20).ToUpperInvariant();
		SubdivisionType = RequireValue(subdivisionType, nameof(subdivisionType), 50);
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
