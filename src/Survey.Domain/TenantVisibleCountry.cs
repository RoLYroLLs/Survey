namespace Survey.Domain;

public class TenantVisibleCountry : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int CountryId { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public Tenant Tenant { get; private set; } = default!;
	public Country Country { get; private set; } = default!;

	private TenantVisibleCountry()
	{
	}

	public TenantVisibleCountry(int tenantId, int countryId)
	{
		if (tenantId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantId));
		}

		if (countryId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(countryId));
		}

		TenantId = tenantId;
		CountryId = countryId;
		CreatedUtc = DateTimeOffset.UtcNow;
	}
}
