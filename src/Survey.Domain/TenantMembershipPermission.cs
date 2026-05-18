namespace Survey.Domain;

public class TenantMembershipPermission
{
	public int Id { get; private set; }
	public int TenantMembershipId { get; private set; }
	public string PermissionKey { get; private set; } = string.Empty;
	public PermissionGrantKind GrantKind { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public TenantMembership Membership { get; private set; } = default!;

	private TenantMembershipPermission()
	{
	}

	public TenantMembershipPermission(int tenantMembershipId, string permissionKey, PermissionGrantKind grantKind)
	{
		if (tenantMembershipId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantMembershipId));
		}

		TenantMembershipId = tenantMembershipId;
		PermissionKey = RequireValue(permissionKey, nameof(permissionKey), 200);
		GrantKind = grantKind;
		CreatedUtc = DateTimeOffset.UtcNow;
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
