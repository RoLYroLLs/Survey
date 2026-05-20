using Hangfire.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Services;

namespace Survey.Infrastructure.Security;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
	public bool Authorize(DashboardContext context)
	{
		var httpContext = context.GetHttpContext();
		if (httpContext.User.Identity?.IsAuthenticated != true)
		{
			return false;
		}

		var tenantContextAccessor = httpContext.RequestServices.GetRequiredService<ITenantContextAccessor>();
		var accessContext = tenantContextAccessor.GetCurrentAsync().GetAwaiter().GetResult();
		return accessContext.IsPlatformSuperAdmin
			|| accessContext.PlatformPermissions.Contains(PlatformPermissionKeys.JobsView, StringComparer.Ordinal)
			|| accessContext.PlatformPermissions.Contains(PlatformPermissionKeys.JobsManage, StringComparer.Ordinal);
	}
}
