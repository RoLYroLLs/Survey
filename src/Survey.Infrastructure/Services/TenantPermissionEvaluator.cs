using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed class TenantPermissionEvaluator(
	ITenantContextAccessor tenantContextAccessor,
	IAuditWriter auditWriter) : ITenantPermissionEvaluator
{
	private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor;
	private readonly IAuditWriter _auditWriter = auditWriter;

	public Task<CurrentAccessContext> RequireAccessAsync(CancellationToken cancellationToken = default)
	{
		return _tenantContextAccessor.RequireTenantContextAsync(cancellationToken);
	}

	public async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
	{
		var context = await _tenantContextAccessor.GetCurrentAsync(cancellationToken);
		return context.HasTenantAccess && context.TenantPermissions.Contains(permissionKey, StringComparer.Ordinal);
	}

	public async Task EnsurePermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
	{
		var context = await _tenantContextAccessor.RequireTenantContextAsync(cancellationToken);
		if (context.TenantPermissions.Contains(permissionKey, StringComparer.Ordinal))
		{
			return;
		}

		await _auditWriter.WriteAsync("tenant", "authorization.denied", "permission", permissionKey, $"Denied tenant permission '{permissionKey}'.", false, cancellationToken);
		throw new UnauthorizedAccessException("You are not authorized to perform this action.");
	}
}
