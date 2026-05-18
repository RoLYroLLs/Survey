using Microsoft.AspNetCore.Authorization;

namespace Survey.Infrastructure.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PlatformAuthorizeAttribute : AuthorizeAttribute
{
	public PlatformAuthorizeAttribute()
	{
		Policy = SurveyAuthorizationPolicies.PlatformAccess;
	}

	public PlatformAuthorizeAttribute(string permissionKey)
	{
		Policy = SurveyAuthorizationPolicies.PlatformPermission(permissionKey);
	}
}
