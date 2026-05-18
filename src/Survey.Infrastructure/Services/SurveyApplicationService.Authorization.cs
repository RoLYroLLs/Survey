using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	private Task RequirePlatformPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
	{
		return _platformPermissionEvaluator.EnsurePermissionAsync(permissionKey, cancellationToken);
	}

	private Task RequireTenantPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
	{
		return _tenantPermissionEvaluator.EnsurePermissionAsync(permissionKey, cancellationToken);
	}

	private Task<CurrentAccessContext> RequireTenantAccessAsync(CancellationToken cancellationToken = default)
	{
		return _tenantPermissionEvaluator.RequireAccessAsync(cancellationToken);
	}

	private async Task<CurrentAccessContext> RequireTenantOwnerAsync(CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantAccessAsync(cancellationToken);
		if (context.TenantRole != TenantRole.Owner)
		{
			throw new UnauthorizedAccessException("Only a tenant owner can perform this action.");
		}

		return context;
	}

	private async Task<int> RequireTenantIdAsync(CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantAccessAsync(cancellationToken);
		return context.TenantId ?? throw new UnauthorizedAccessException("An active tenant is required.");
	}

	private async Task<CurrentAccessContext> RequirePlatformAccessAsync(CancellationToken cancellationToken = default)
	{
		return await _tenantContextAccessor.RequirePlatformContextAsync(cancellationToken);
	}

	private async Task<string> RequireCurrentUserIdAsync(CancellationToken cancellationToken = default)
	{
		var context = await _tenantContextAccessor.GetCurrentAsync(cancellationToken);
		if (!context.IsAuthenticated || string.IsNullOrWhiteSpace(context.UserId))
		{
			throw new UnauthorizedAccessException("Authentication is required.");
		}

		return context.UserId;
	}

	private Task AuditTenantEntityChangeAsync(string actionType, string targetType, int? targetId, string details, CancellationToken cancellationToken = default)
	{
		return _auditWriter.WriteAsync("tenant", actionType, targetType, targetId?.ToString(), details, true, cancellationToken);
	}

	private Task AuditTenantEntityChangeAsync(string actionType, string targetType, string? targetId, string details, CancellationToken cancellationToken = default)
	{
		return _auditWriter.WriteAsync("tenant", actionType, targetType, targetId, details, true, cancellationToken);
	}

	private Task AuditPlatformEntityChangeAsync(string actionType, string targetType, string? targetId, string details, CancellationToken cancellationToken = default)
	{
		return _auditWriter.WriteAsync("platform", actionType, targetType, targetId, details, true, cancellationToken);
	}
}
