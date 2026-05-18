using Survey.Application.Models;

namespace Survey.Application.Services;

public interface ITenantPermissionEvaluator
{
	Task<CurrentAccessContext> RequireAccessAsync(CancellationToken cancellationToken = default);
	Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default);
	Task EnsurePermissionAsync(string permissionKey, CancellationToken cancellationToken = default);
}
