namespace Survey.Domain;

public static class TenantPermissionKeys
{
	public const string DashboardView = "dashboard.view";
	public const string PeopleView = "people.view";
	public const string PeopleCreate = "people.create";
	public const string PeopleEdit = "people.edit";
	public const string PeopleDelete = "people.delete";
	public const string LocationsView = "locations.view";
	public const string LocationsCreate = "locations.create";
	public const string LocationsEdit = "locations.edit";
	public const string LocationsDelete = "locations.delete";
	public const string AddressesView = "addresses.view";
	public const string AddressesCreate = "addresses.create";
	public const string AddressesEdit = "addresses.edit";
	public const string AddressesDelete = "addresses.delete";
	public const string AreasView = "areas.view";
	public const string AreasCreate = "areas.create";
	public const string AreasEdit = "areas.edit";
	public const string AreasDelete = "areas.delete";
	public const string GoalsView = "goals.view";
	public const string GoalsCreate = "goals.create";
	public const string GoalsEdit = "goals.edit";
	public const string GoalsDelete = "goals.delete";
	public const string SurveysView = "surveys.view";
	public const string SurveysCreate = "surveys.create";
	public const string SurveysEdit = "surveys.edit";
	public const string SurveysDelete = "surveys.delete";
	public const string AssignmentsView = "assignments.view";
	public const string AssignmentsCreate = "assignments.create";
	public const string AssignmentsEdit = "assignments.edit";
	public const string AssignmentsDelete = "assignments.delete";
	public const string AssignmentsArchive = "assignments.archive";
	public const string AssignmentsFill = "assignments.fill";
	public const string AssignmentsSend = "assignments.send";
	public const string ResponsesView = "responses.view";
	public const string ResponsesExport = "responses.export";
	public const string ReportsView = "reports.view";
	public const string ReportsExport = "reports.export";
	public const string SettingsView = "settings.view";
	public const string SettingsManage = "settings.manage";
	public const string UsersView = "users.view";
	public const string UsersInvite = "users.invite";
	public const string UsersChangeRole = "users.change-role";
	public const string UsersManagePermissions = "users.manage-permissions";
	public const string UsersEnableDisable = "users.enable-disable";
	public const string UsersRemove = "users.remove";
	public const string UsersReviewEffectivePermissions = "users.review-effective-permissions";

	public static readonly IReadOnlyList<string> All =
	[
		DashboardView,
		PeopleView,
		PeopleCreate,
		PeopleEdit,
		PeopleDelete,
		LocationsView,
		LocationsCreate,
		LocationsEdit,
		LocationsDelete,
		AddressesView,
		AddressesCreate,
		AddressesEdit,
		AddressesDelete,
		AreasView,
		AreasCreate,
		AreasEdit,
		AreasDelete,
		GoalsView,
		GoalsCreate,
		GoalsEdit,
		GoalsDelete,
		SurveysView,
		SurveysCreate,
		SurveysEdit,
		SurveysDelete,
		AssignmentsView,
		AssignmentsCreate,
		AssignmentsEdit,
		AssignmentsDelete,
		AssignmentsArchive,
		AssignmentsFill,
		AssignmentsSend,
		ResponsesView,
		ResponsesExport,
		ReportsView,
		ReportsExport,
		SettingsView,
		SettingsManage,
		UsersView,
		UsersInvite,
		UsersChangeRole,
		UsersManagePermissions,
		UsersEnableDisable,
		UsersRemove,
		UsersReviewEffectivePermissions
	];
}
