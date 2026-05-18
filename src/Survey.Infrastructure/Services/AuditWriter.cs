using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Persistence;
using Survey.Infrastructure.Security;

namespace Survey.Infrastructure.Services;

public sealed class AuditWriter(
	SurveyDbContext dbContext,
	ITenantContextAccessor tenantContextAccessor,
	TenantExecutionContext tenantExecutionContext) : IAuditWriter
{
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor;
	private readonly TenantExecutionContext _tenantExecutionContext = tenantExecutionContext;

	public async Task WriteAsync(
		string plane,
		string actionType,
		string targetType,
		string? targetId,
		string? details,
		bool succeeded,
		CancellationToken cancellationToken = default)
	{
		var context = await _tenantContextAccessor.GetCurrentAsync(cancellationToken);
		var tenantId = string.Equals(plane, "tenant", StringComparison.OrdinalIgnoreCase)
			? context.TenantId ?? _tenantExecutionContext.TenantId
			: null;

		_dbContext.AuditLogs.Add(new AuditLog(
			tenantId,
			context.IsAuthenticated ? context.UserId : null,
			plane,
			actionType,
			targetType,
			targetId,
			details,
			succeeded));

		await _dbContext.SaveChangesAsync(cancellationToken);
	}
}
