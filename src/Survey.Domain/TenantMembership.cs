namespace Survey.Domain;

public class TenantMembership
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public string UserId { get; private set; } = string.Empty;
	public TenantRole Role { get; private set; }
	public bool IsEnabled { get; private set; } = true;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public Tenant Tenant { get; private set; } = default!;
	public ICollection<TenantMembershipPermission> PermissionOverrides { get; } = new List<TenantMembershipPermission>();

	private TenantMembership()
	{
	}

	public TenantMembership(int tenantId, string userId, TenantRole role)
	{
		if (tenantId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantId));
		}

		TenantId = tenantId;
		UserId = RequireValue(userId, nameof(userId), 450);
		Role = role;
		CreatedUtc = DateTimeOffset.UtcNow;
		UpdatedUtc = CreatedUtc;
	}

	public void ChangeRole(TenantRole role)
	{
		Role = role;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
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
