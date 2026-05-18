namespace Survey.Application.Services;

public interface IPlatformPermissionEvaluator
{
	Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default);
	Task EnsurePermissionAsync(string permissionKey, CancellationToken cancellationToken = default);
}
