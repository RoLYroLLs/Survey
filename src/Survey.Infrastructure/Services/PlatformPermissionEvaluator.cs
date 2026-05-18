using Survey.Application.Services;

namespace Survey.Infrastructure.Services;

public sealed class PlatformPermissionEvaluator(
	ITenantContextAccessor tenantContextAccessor,
	IAuditWriter auditWriter) : IPlatformPermissionEvaluator
{
	private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor;
	private readonly IAuditWriter _auditWriter = auditWriter;

	public async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
	{
		var context = await _tenantContextAccessor.GetCurrentAsync(cancellationToken);
		return context.IsPlatformUserEnabled && context.PlatformPermissions.Contains(permissionKey, StringComparer.Ordinal);
	}

	public async Task EnsurePermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
	{
		var context = await _tenantContextAccessor.RequirePlatformContextAsync(cancellationToken);
		if (context.PlatformPermissions.Contains(permissionKey, StringComparer.Ordinal))
		{
			return;
		}

		await _auditWriter.WriteAsync("platform", "authorization.denied", "permission", permissionKey, $"Denied platform permission '{permissionKey}'.", false, cancellationToken);
		throw new UnauthorizedAccessException("You are not authorized to perform this action.");
	}
}
