namespace Survey.Domain;

public class AreaCounty
{
	public int Id { get; private set; }
	public int AreaId { get; private set; }
	public string CountyFips { get; private set; } = string.Empty;
	public string CountyName { get; private set; } = string.Empty;
	public string StateCode { get; private set; } = string.Empty;
	public Area Area { get; private set; } = default!;

	private AreaCounty()
	{
	}

	public AreaCounty(int areaId, string countyFips, string countyName, string stateCode)
	{
		AreaId = areaId;
		CountyFips = RequireValue(countyFips, nameof(countyFips), 5);
		CountyName = RequireValue(countyName, nameof(countyName), 200);
		StateCode = RequireValue(stateCode, nameof(stateCode), 2).ToUpperInvariant();
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
