namespace Survey.Domain;

public class TenantVisibleStateProvince : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public int StateProvinceId { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public Tenant Tenant { get; private set; } = default!;
	public StateProvince StateProvince { get; private set; } = default!;

	private TenantVisibleStateProvince()
	{
	}

	public TenantVisibleStateProvince(int tenantId, int stateProvinceId)
	{
		if (tenantId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantId));
		}

		if (stateProvinceId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(stateProvinceId));
		}

		TenantId = tenantId;
		StateProvinceId = stateProvinceId;
		CreatedUtc = DateTimeOffset.UtcNow;
	}
}
