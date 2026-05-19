using System.ComponentModel.DataAnnotations;
using Survey.Domain;

namespace Survey.Application.Models;

public sealed class PlatformUserListItem
{
	public string Id { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public bool IsPlatformUserEnabled { get; set; }
	public bool IsPlatformSuperAdmin { get; set; }
	public int PermissionCount { get; set; }
}

public sealed class PlatformUserPermissionEditModel
{
	public string PermissionKey { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string PermissionLabel { get; set; } = string.Empty;
	public bool Selected { get; set; }
}

public sealed class PlatformUserEditModel
{
	public string? Id { get; set; }

	[Required]
	[StringLength(100)]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	[StringLength(100)]
	[DataType(DataType.Password)]
	public string? Password { get; set; }

	[StringLength(100)]
	[DataType(DataType.Password)]
	[Compare(nameof(Password))]
	public string? ConfirmPassword { get; set; }

	public bool IsPlatformUserEnabled { get; set; }
	public bool IsPlatformSuperAdmin { get; set; }
	public bool IsBootstrapPlatformOwner { get; set; }
	public bool IsCurrentUser { get; set; }
	public IReadOnlyList<PlatformUserPermissionEditModel> Permissions { get; set; } = Array.Empty<PlatformUserPermissionEditModel>();
	public bool IsNew => string.IsNullOrWhiteSpace(Id);
}

public sealed class PlatformUserInviteModel
{
	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	public bool IsPlatformUserEnabled { get; set; } = true;
	public bool IsPlatformSuperAdmin { get; set; }
	public int? TenantId { get; set; }
	public TenantRole TenantRole { get; set; } = TenantRole.User;
	public IReadOnlyList<PlatformUserPermissionEditModel> Permissions { get; set; } = Array.Empty<PlatformUserPermissionEditModel>();
}

public sealed class PlatformUserInviteResultModel
{
	public string Token { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public DateTimeOffset ExpiresAtUtc { get; set; }
	public string InvitationUrl { get; set; } = string.Empty;
}

public sealed class PlatformUserInvitationAcceptanceContextModel
{
	public string Token { get; set; } = string.Empty;
	public bool IsValid { get; set; }
	public string? ErrorMessage { get; set; }
	public string Email { get; set; } = string.Empty;
	public bool IsPlatformUserEnabled { get; set; }
	public bool IsPlatformSuperAdmin { get; set; }
	public bool ExistingAccountFound { get; set; }
	public string? TenantName { get; set; }
	public TenantRole? TenantRole { get; set; }
	public DateTimeOffset? ExpiresAtUtc { get; set; }
	public IReadOnlyList<string> PermissionLabels { get; set; } = Array.Empty<string>();
}

public sealed class PlatformTenantListItem
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;
	public int MembershipCount { get; set; }
	public int EnabledMembershipCount { get; set; }
	public int OwnerCount { get; set; }
	public string OwnerDisplayName { get; set; } = string.Empty;
	public string OwnerEmail { get; set; } = string.Empty;
	public int PendingInvitationCount { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset UpdatedUtc { get; set; }
}

public sealed class PlatformTenantMembershipListItem
{
	public int MembershipId { get; set; }
	public string UserId { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public TenantRole Role { get; set; }
	public bool IsEnabled { get; set; }
	public int PermissionOverrideCount { get; set; }
	public int EffectivePermissionCount { get; set; }
}

public sealed class PlatformTenantDetailModel
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;
	public string ThemePresetKey { get; set; } = string.Empty;
	public int MembershipCount { get; set; }
	public int EnabledMembershipCount { get; set; }
	public int OwnerCount { get; set; }
	public string OwnerDisplayName { get; set; } = string.Empty;
	public string OwnerEmail { get; set; } = string.Empty;
	public int PendingInvitationCount { get; set; }
	public int PeopleCount { get; set; }
	public int LocationCount { get; set; }
	public int SurveyCount { get; set; }
	public int AssignmentCount { get; set; }
	public int ResponseCount { get; set; }
	public int GoalCount { get; set; }
	public int AreaCount { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset UpdatedUtc { get; set; }
	public IReadOnlyList<PlatformTenantMembershipListItem> Memberships { get; set; } = Array.Empty<PlatformTenantMembershipListItem>();
}

public sealed class PlatformTenantEditModel
{
	public int Id { get; set; }

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	public string Slug { get; set; } = string.Empty;
	public string ThemePresetKey { get; set; } = string.Empty;
	public DateTimeOffset UpdatedUtc { get; set; }
}

public sealed class AuditLogListItem
{
	public int Id { get; set; }
	public int? TenantId { get; set; }
	public string? TenantName { get; set; }
	public string? ActorUserId { get; set; }
	public string ActorDisplayName { get; set; } = string.Empty;
	public string Plane { get; set; } = string.Empty;
	public string ActionType { get; set; } = string.Empty;
	public string TargetType { get; set; } = string.Empty;
	public string? TargetId { get; set; }
	public string? Details { get; set; }
	public bool Succeeded { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
}
