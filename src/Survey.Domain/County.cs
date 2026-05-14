namespace Survey.Domain;

public class County
{
	public int Id { get; private set; }
	public int StateProvinceId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string FipsCode { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public StateProvince StateProvince { get; private set; } = default!;
	public ICollection<PostalAddress> PostalAddresses { get; } = new List<PostalAddress>();

	private County()
	{
	}

	public County(int stateProvinceId, string name, string fipsCode)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(stateProvinceId, name, fipsCode);
	}

	public void Update(int stateProvinceId, string name, string fipsCode)
	{
		if (stateProvinceId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(stateProvinceId));
		}

		StateProvinceId = stateProvinceId;
		Name = RequireValue(name, nameof(name), 200);
		FipsCode = RequireValue(fipsCode, nameof(fipsCode), 20).ToUpperInvariant();
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
