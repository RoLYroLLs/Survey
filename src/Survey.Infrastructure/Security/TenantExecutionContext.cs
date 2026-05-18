namespace Survey.Infrastructure.Security;

public sealed class TenantExecutionContext
{
	public int? TenantId { get; private set; }

	public bool BypassTenantIsolation { get; private set; }

	public void UseTenant(int tenantId)
	{
		if (tenantId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantId));
		}

		TenantId = tenantId;
		BypassTenantIsolation = false;
	}

	public void UsePlatformBypass()
	{
		TenantId = null;
		BypassTenantIsolation = true;
	}

	public void Clear()
	{
		TenantId = null;
		BypassTenantIsolation = false;
	}
}
