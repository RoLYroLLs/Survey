namespace Survey.Application.Services;

public interface IAuditWriter
{
	Task WriteAsync(
		string plane,
		string actionType,
		string targetType,
		string? targetId,
		string? details,
		bool succeeded,
		CancellationToken cancellationToken = default);
}
