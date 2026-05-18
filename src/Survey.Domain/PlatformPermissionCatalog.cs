namespace Survey.Domain;

public sealed record PlatformPermissionDefinition(
	string Key,
	string Category,
	string Label);

public static class PlatformPermissionCatalog
{
	public static readonly IReadOnlyList<PlatformPermissionDefinition> All =
	[
		new(PlatformPermissionKeys.UsersView, "Users", "View"),
		new(PlatformPermissionKeys.UsersManage, "Users", "Manage"),
		new(PlatformPermissionKeys.PermissionsManage, "Users", "Manage permissions"),
		new(PlatformPermissionKeys.TenantsView, "Tenants", "View"),
		new(PlatformPermissionKeys.TenantsManage, "Tenants", "Manage"),
		new(PlatformPermissionKeys.TenantsOversight, "Tenants", "Oversight"),
		new(PlatformPermissionKeys.GeographyView, "Geography", "View"),
		new(PlatformPermissionKeys.GeographyManage, "Geography", "Manage"),
		new(PlatformPermissionKeys.AuditView, "Audit", "View"),
		new(PlatformPermissionKeys.SettingsManage, "Settings", "Manage")
	];

	public static PlatformPermissionDefinition Get(string permissionKey)
	{
		return All.FirstOrDefault(definition => string.Equals(definition.Key, permissionKey, StringComparison.Ordinal))
			?? new PlatformPermissionDefinition(permissionKey, "Other", permissionKey);
	}
}
