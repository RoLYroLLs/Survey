namespace Survey.Domain;

public class TenantInvitation
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public string Email { get; private set; } = string.Empty;
	public TenantRole Role { get; private set; }
	public string TokenHash { get; private set; } = string.Empty;
	public DateTimeOffset ExpiresAtUtc { get; private set; }
	public string CreatedByUserId { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset? AcceptedUtc { get; private set; }
	public DateTimeOffset? RevokedUtc { get; private set; }
	public Tenant Tenant { get; private set; } = default!;

	private TenantInvitation()
	{
	}

	public TenantInvitation(int tenantId, string email, TenantRole role, string tokenHash, DateTimeOffset expiresAtUtc, string createdByUserId)
	{
		if (tenantId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantId));
		}

		if (expiresAtUtc <= DateTimeOffset.UtcNow)
		{
			throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));
		}

		TenantId = tenantId;
		Email = RequireValue(email, nameof(email), 256);
		Role = role;
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
