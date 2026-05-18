using Microsoft.AspNetCore.Authorization;

namespace Survey.Infrastructure.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class TenantAuthorizeAttribute : AuthorizeAttribute
{
	public TenantAuthorizeAttribute()
	{
		Policy = SurveyAuthorizationPolicies.TenantAccess;
	}

	public TenantAuthorizeAttribute(string permissionKey)
	{
		Policy = SurveyAuthorizationPolicies.TenantPermission(permissionKey);
	}
}
