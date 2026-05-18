namespace Survey.Domain;

public static class PlatformPermissionKeys
{
	public const string UsersView = "platform.users.view";
	public const string UsersManage = "platform.users.manage";
	public const string PermissionsManage = "platform.permissions.manage";
	public const string TenantsView = "platform.tenants.view";
	public const string TenantsManage = "platform.tenants.manage";
	public const string TenantsOversight = "platform.tenants.oversight";
	public const string GeographyView = "platform.geography.view";
	public const string GeographyManage = "platform.geography.manage";
	public const string AuditView = "platform.audit.view";
	public const string SettingsManage = "platform.settings.manage";

	public static readonly IReadOnlyList<string> All =
	[
		UsersView,
		UsersManage,
		PermissionsManage,
		TenantsView,
		TenantsManage,
		TenantsOversight,
		GeographyView,
		GeographyManage,
		AuditView,
		SettingsManage
	];
}
