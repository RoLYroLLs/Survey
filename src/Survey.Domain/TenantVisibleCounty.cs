namespace Survey.Domain;

public class TenantVisibleCounty : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int CountyId { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public Tenant Tenant { get; private set; } = default!;
	public County County { get; private set; } = default!;

	private TenantVisibleCounty()
	{
	}

	public TenantVisibleCounty(int tenantId, int countyId)
	{
		if (tenantId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantId));
		}

		if (countyId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(countyId));
		}

		TenantId = tenantId;
		CountyId = countyId;
		CreatedUtc = DateTimeOffset.UtcNow;
	}
}
