namespace Survey.Domain;

public static class PermissionDefaults
{
	private static readonly IReadOnlySet<string> OwnerPermissions = TenantPermissionKeys.All.ToHashSet(StringComparer.Ordinal);
	private static readonly IReadOnlySet<string> AdminPermissions = new HashSet<string>(
	[
		TenantPermissionKeys.DashboardView,
		TenantPermissionKeys.PeopleView,
		TenantPermissionKeys.PeopleCreate,
		TenantPermissionKeys.PeopleEdit,
		TenantPermissionKeys.PeopleDelete,
		TenantPermissionKeys.LocationsView,
		TenantPermissionKeys.LocationsCreate,
		TenantPermissionKeys.LocationsEdit,
		TenantPermissionKeys.LocationsDelete,
		TenantPermissionKeys.AddressesView,
		TenantPermissionKeys.AddressesCreate,
		TenantPermissionKeys.AddressesEdit,
		TenantPermissionKeys.AddressesDelete,
		TenantPermissionKeys.AreasView,
		TenantPermissionKeys.AreasCreate,
		TenantPermissionKeys.AreasEdit,
		TenantPermissionKeys.AreasDelete,
		TenantPermissionKeys.GoalsView,
		TenantPermissionKeys.GoalsCreate,
		TenantPermissionKeys.GoalsEdit,
		TenantPermissionKeys.GoalsDelete,
		TenantPermissionKeys.SurveysView,
		TenantPermissionKeys.SurveysCreate,
		TenantPermissionKeys.SurveysEdit,
		TenantPermissionKeys.SurveysDelete,
		TenantPermissionKeys.AssignmentsView,
		TenantPermissionKeys.AssignmentsCreate,
		TenantPermissionKeys.AssignmentsEdit,
		TenantPermissionKeys.AssignmentsDelete,
		TenantPermissionKeys.AssignmentsArchive,
		TenantPermissionKeys.AssignmentsFill,
		TenantPermissionKeys.ResponsesView,
		TenantPermissionKeys.ResponsesExport,
		TenantPermissionKeys.ReportsView,
		TenantPermissionKeys.ReportsExport,
		TenantPermissionKeys.SettingsView,
		TenantPermissionKeys.SettingsManage,
		TenantPermissionKeys.UsersView,
		TenantPermissionKeys.UsersInvite,
		TenantPermissionKeys.UsersChangeRole,
		TenantPermissionKeys.UsersManagePermissions,
		TenantPermissionKeys.UsersEnableDisable,
		TenantPermissionKeys.UsersRemove,
		TenantPermissionKeys.UsersReviewEffectivePermissions
	], StringComparer.Ordinal);
	private static readonly IReadOnlySet<string> UserPermissions = new HashSet<string>(
	[
		TenantPermissionKeys.DashboardView,
		TenantPermissionKeys.AssignmentsView,
		TenantPermissionKeys.AssignmentsFill,
		TenantPermissionKeys.SettingsView
	], StringComparer.Ordinal);
	private static readonly IReadOnlySet<string> PlatformAdminPermissions = PlatformPermissionKeys.All.ToHashSet(StringComparer.Ordinal);

	public static IReadOnlySet<string> GetTenantPermissions(TenantRole role)
	{
		return role switch
		{
			TenantRole.Owner => OwnerPermissions,
			TenantRole.Admin => AdminPermissions,
			_ => UserPermissions
		};
	}

	public static IReadOnlySet<string> GetMigratedEmployeePermissions()
	{
		return UserPermissions;
	}

	public static IReadOnlySet<string> GetPlatformAdminPermissions()
	{
		return PlatformAdminPermissions;
	}
}
