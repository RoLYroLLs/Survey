using Survey.Domain;

namespace Survey.Application.Models;

public sealed class CurrentAccessContext
{
	public string UserId { get; init; } = string.Empty;
	public string? Email { get; init; }
	public bool IsAuthenticated { get; init; }
	public bool IsPlatformSuperAdmin { get; init; }
	public bool IsPlatformUserEnabled { get; init; }
	public int? ActiveTenantMembershipId { get; init; }
	public int? TenantId { get; init; }
	public string? TenantName { get; init; }
	public TenantRole? TenantRole { get; init; }
	public bool TenantMembershipEnabled { get; init; }
	public IReadOnlySet<string> TenantPermissions { get; init; } = new HashSet<string>(StringComparer.Ordinal);
	public IReadOnlySet<string> PlatformPermissions { get; init; } = new HashSet<string>(StringComparer.Ordinal);

	public bool HasTenantAccess => TenantId.HasValue && ActiveTenantMembershipId.HasValue && TenantMembershipEnabled;
}

public sealed class TenantMembershipOption
{
	public int MembershipId { get; init; }
	public int TenantId { get; init; }
	public string TenantName { get; init; } = string.Empty;
	public TenantRole Role { get; init; }
	public bool IsEnabled { get; init; }
	public bool IsActive { get; init; }
}
