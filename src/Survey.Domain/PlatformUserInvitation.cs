namespace Survey.Domain;

public class PlatformUserInvitation
{
	public int Id { get; private set; }
	public string Email { get; private set; } = string.Empty;
	public bool IsPlatformUserEnabled { get; private set; }
	public bool IsPlatformSuperAdmin { get; private set; }
	public string PermissionKeysJson { get; private set; } = "[]";
	public int? TenantId { get; private set; }
	public TenantRole? TenantRole { get; private set; }
	public string TokenHash { get; private set; } = string.Empty;
	public DateTimeOffset ExpiresAtUtc { get; private set; }
	public string CreatedByUserId { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset? AcceptedUtc { get; private set; }
	public DateTimeOffset? RevokedUtc { get; private set; }
	public Tenant? Tenant { get; private set; }

	private PlatformUserInvitation()
	{
	}

	public PlatformUserInvitation(
		string email,
		bool isPlatformUserEnabled,
		bool isPlatformSuperAdmin,
		string permissionKeysJson,
		int? tenantId,
		TenantRole? tenantRole,
		string tokenHash,
		DateTimeOffset expiresAtUtc,
		string createdByUserId)
	{
		if (expiresAtUtc <= DateTimeOffset.UtcNow)
		{
			throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));
		}

		Email = RequireValue(email, nameof(email), 256);
		IsPlatformUserEnabled = isPlatformUserEnabled;
		IsPlatformSuperAdmin = isPlatformSuperAdmin;
		PermissionKeysJson = RequireValue(permissionKeysJson, nameof(permissionKeysJson), 4000);
		TenantId = tenantId;
		TenantRole = tenantRole;
		TokenHash = RequireValue(tokenHash, nameof(tokenHash), 512);
		ExpiresAtUtc = expiresAtUtc;
		CreatedByUserId = RequireValue(createdByUserId, nameof(createdByUserId), 450);
		CreatedUtc = DateTimeOffset.UtcNow;
	}

	public void Accept()
	{
		AcceptedUtc = DateTimeOffset.UtcNow;
	}

	public void Revoke()
	{
		RevokedUtc = DateTimeOffset.UtcNow;
	}

	public bool IsUsable(DateTimeOffset nowUtc)
	{
		return AcceptedUtc is null && RevokedUtc is null && ExpiresAtUtc > nowUtc;
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
