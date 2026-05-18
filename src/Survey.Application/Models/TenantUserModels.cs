using System.ComponentModel.DataAnnotations;
using Survey.Domain;

namespace Survey.Application.Models;

public static class TenantPermissionOverrideModes
{
	public const string Default = "default";
	public const string Allow = "allow";
	public const string Deny = "deny";
}

public sealed class TenantUserListItem
{
	public int MembershipId { get; set; }
	public string UserId { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public TenantRole Role { get; set; }
	public bool IsEnabled { get; set; }
	public bool IsCurrentUser { get; set; }
	public int PermissionOverrideCount { get; set; }
	public int EffectivePermissionCount { get; set; }
}

public sealed class TenantUserPermissionOverrideEditModel
{
	public string PermissionKey { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string PermissionLabel { get; set; } = string.Empty;
	public bool DefaultGranted { get; set; }
	public string OverrideMode { get; set; } = TenantPermissionOverrideModes.Default;
	public bool EffectiveGranted => OverrideMode switch
	{
		TenantPermissionOverrideModes.Allow => true,
		TenantPermissionOverrideModes.Deny => false,
		_ => DefaultGranted
	};
}

public sealed class TenantUserEditModel
{
	public int MembershipId { get; set; }
	public string UserId { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public TenantRole Role { get; set; }
	public bool IsEnabled { get; set; }
	public bool IsCurrentUser { get; set; }
	public bool CanChangeRole { get; set; }
	public bool CanManagePermissions { get; set; }
	public bool CanEnableDisable { get; set; }
	public bool CanRemove { get; set; }
	public bool CanReviewEffectivePermissions { get; set; }
	public IReadOnlyList<TenantUserPermissionOverrideEditModel> Permissions { get; set; } = Array.Empty<TenantUserPermissionOverrideEditModel>();
}

public sealed class TenantUserInviteModel
{
	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	[Required]
	public TenantRole Role { get; set; } = TenantRole.User;
}

public sealed class TenantInvitationCreateResultModel
{
	public string Token { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public TenantRole Role { get; set; }
	public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class TenantInvitationAcceptanceContextModel
{
	public string Token { get; set; } = string.Empty;
	public bool IsValid { get; set; }
	public string? ErrorMessage { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public TenantRole Role { get; set; }
	public bool ExistingAccountFound { get; set; }
	public DateTimeOffset? ExpiresAtUtc { get; set; }
}

public sealed class TenantInvitationListItem
{
	public int Id { get; set; }
	public string Email { get; set; } = string.Empty;
	public TenantRole Role { get; set; }
	public string CreatedByDisplayName { get; set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset ExpiresAtUtc { get; set; }
	public DateTimeOffset? AcceptedUtc { get; set; }
	public DateTimeOffset? RevokedUtc { get; set; }
	public bool IsPending { get; set; }
	public bool IsExpired { get; set; }
}

public sealed class TenantInvitationRegistrationModel
{
	[Required]
	public string Token { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	[DataType(DataType.Password)]
	[Compare(nameof(Password))]
	public string ConfirmPassword { get; set; } = string.Empty;
}
