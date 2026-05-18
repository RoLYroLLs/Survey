namespace Survey.Domain;

public sealed record TenantPermissionDefinition(
	string Key,
	string Category,
	string Label);

public static class TenantPermissionCatalog
{
	public static readonly IReadOnlyList<TenantPermissionDefinition> All =
	[
		new(TenantPermissionKeys.DashboardView, "Dashboard", "View"),
		new(TenantPermissionKeys.PeopleView, "People", "View"),
		new(TenantPermissionKeys.PeopleCreate, "People", "Create"),
		new(TenantPermissionKeys.PeopleEdit, "People", "Edit"),
		new(TenantPermissionKeys.PeopleDelete, "People", "Delete"),
		new(TenantPermissionKeys.LocationsView, "Locations", "View"),
		new(TenantPermissionKeys.LocationsCreate, "Locations", "Create"),
		new(TenantPermissionKeys.LocationsEdit, "Locations", "Edit"),
		new(TenantPermissionKeys.LocationsDelete, "Locations", "Delete"),
		new(TenantPermissionKeys.AddressesView, "Addresses", "View"),
		new(TenantPermissionKeys.AddressesCreate, "Addresses", "Create"),
		new(TenantPermissionKeys.AddressesEdit, "Addresses", "Edit"),
		new(TenantPermissionKeys.AddressesDelete, "Addresses", "Delete"),
		new(TenantPermissionKeys.AreasView, "Areas", "View"),
		new(TenantPermissionKeys.AreasCreate, "Areas", "Create"),
		new(TenantPermissionKeys.AreasEdit, "Areas", "Edit"),
		new(TenantPermissionKeys.AreasDelete, "Areas", "Delete"),
		new(TenantPermissionKeys.GoalsView, "Goals", "View"),
		new(TenantPermissionKeys.GoalsCreate, "Goals", "Create"),
		new(TenantPermissionKeys.GoalsEdit, "Goals", "Edit"),
		new(TenantPermissionKeys.GoalsDelete, "Goals", "Delete"),
		new(TenantPermissionKeys.SurveysView, "Surveys", "View"),
		new(TenantPermissionKeys.SurveysCreate, "Surveys", "Create"),
		new(TenantPermissionKeys.SurveysEdit, "Surveys", "Edit"),
		new(TenantPermissionKeys.SurveysDelete, "Surveys", "Delete"),
		new(TenantPermissionKeys.AssignmentsView, "Assignments", "View"),
		new(TenantPermissionKeys.AssignmentsCreate, "Assignments", "Create"),
		new(TenantPermissionKeys.AssignmentsEdit, "Assignments", "Edit"),
		new(TenantPermissionKeys.AssignmentsDelete, "Assignments", "Delete"),
		new(TenantPermissionKeys.AssignmentsArchive, "Assignments", "Archive"),
		new(TenantPermissionKeys.AssignmentsFill, "Assignments", "Fill"),
		new(TenantPermissionKeys.ResponsesView, "Responses", "View"),
		new(TenantPermissionKeys.ResponsesExport, "Responses", "Export"),
		new(TenantPermissionKeys.ReportsView, "Reports", "View"),
		new(TenantPermissionKeys.ReportsExport, "Reports", "Export"),
		new(TenantPermissionKeys.SettingsView, "Settings", "View"),
		new(TenantPermissionKeys.SettingsManage, "Settings", "Manage"),
		new(TenantPermissionKeys.UsersView, "Users", "View"),
		new(TenantPermissionKeys.UsersInvite, "Users", "Invite"),
		new(TenantPermissionKeys.UsersChangeRole, "Users", "Change role"),
		new(TenantPermissionKeys.UsersManagePermissions, "Users", "Manage permissions"),
		new(TenantPermissionKeys.UsersEnableDisable, "Users", "Enable or disable"),
		new(TenantPermissionKeys.UsersRemove, "Users", "Remove"),
		new(TenantPermissionKeys.UsersReviewEffectivePermissions, "Users", "Review effective permissions")
	];

	public static TenantPermissionDefinition Get(string permissionKey)
	{
		return All.FirstOrDefault(definition => string.Equals(definition.Key, permissionKey, StringComparison.Ordinal))
			?? new TenantPermissionDefinition(permissionKey, "Other", permissionKey);
	}
}
