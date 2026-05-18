using Microsoft.AspNetCore.Authorization;
using Survey.Application.Services;

namespace Survey.Infrastructure.Security;

public sealed class TenantPermissionAuthorizationHandler(
	ITenantPermissionEvaluator tenantPermissionEvaluator,
	ITenantContextAccessor tenantContextAccessor) : AuthorizationHandler<TenantPermissionRequirement>
{
	private readonly ITenantPermissionEvaluator _tenantPermissionEvaluator = tenantPermissionEvaluator;
	private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor;

	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantPermissionRequirement requirement)
	{
		if (context.User.Identity?.IsAuthenticated != true)
		{
			return;
		}

		var hasPermission = string.IsNullOrWhiteSpace(requirement.PermissionKey)
			? (await _tenantContextAccessor.GetCurrentAsync()).HasTenantAccess
			: await _tenantPermissionEvaluator.HasPermissionAsync(requirement.PermissionKey);

		if (hasPermission)
		{
			context.Succeed(requirement);
		}
	}
}
