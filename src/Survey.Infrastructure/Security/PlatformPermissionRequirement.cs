using Microsoft.AspNetCore.Authorization;

namespace Survey.Infrastructure.Security;

public sealed class PlatformPermissionRequirement(string? permissionKey) : IAuthorizationRequirement
{
	public string? PermissionKey { get; } = permissionKey;
}
