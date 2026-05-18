using Microsoft.AspNetCore.Authorization;

namespace Survey.Infrastructure.Security;

public sealed class TenantPermissionRequirement(string? permissionKey) : IAuthorizationRequirement
{
	public string? PermissionKey { get; } = permissionKey;
}
