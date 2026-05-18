namespace Survey.Infrastructure.Security;

public static class SurveyAuthorizationPolicies
{
	public const string TenantAccess = "tenant.access";
	public const string PlatformAccess = "platform.access";
	private const string TenantPrefix = "tenant.permission:";
	private const string PlatformPrefix = "platform.permission:";

	public static string TenantPermission(string permissionKey)
	{
		return $"{TenantPrefix}{permissionKey}";
	}

	public static string PlatformPermission(string permissionKey)
	{
		return $"{PlatformPrefix}{permissionKey}";
	}
}
