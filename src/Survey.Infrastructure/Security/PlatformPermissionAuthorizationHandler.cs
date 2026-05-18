using Microsoft.AspNetCore.Authorization;
using Survey.Application.Services;
using Survey.Domain;

namespace Survey.Infrastructure.Security;

public sealed class PlatformPermissionAuthorizationHandler(
	IPlatformPermissionEvaluator platformPermissionEvaluator,
	ITenantContextAccessor tenantContextAccessor) : AuthorizationHandler<PlatformPermissionRequirement>
{
	private readonly IPlatformPermissionEvaluator _platformPermissionEvaluator = platformPermissionEvaluator;
	private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor;

	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PlatformPermissionRequirement requirement)
	{
		if (context.User.Identity?.IsAuthenticated != true)
		{
			return;
		}

		var accessContext = await _tenantContextAccessor.GetCurrentAsync();
		var hasPermission = string.IsNullOrWhiteSpace(requirement.PermissionKey)
			? accessContext.IsPlatformUserEnabled && accessContext.PlatformPermissions.Count > 0
			: await _platformPermissionEvaluator.HasPermissionAsync(requirement.PermissionKey);

		if (hasPermission)
		{
			context.Succeed(requirement);
		}
	}
}
