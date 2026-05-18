using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605170002_AddMultiTenantFoundation")]
public class AddMultiTenantFoundation : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<int>(
			name: "ActiveTenantMembershipId",
			table: "AspNetUsers",
			type: "int",
			nullable: true);
		migrationBuilder.AddColumn<bool>(
			name: "IsPlatformSuperAdmin",
			table: "AspNetUsers",
			type: "bit",
			nullable: false,
			defaultValue: false);
		migrationBuilder.AddColumn<bool>(
			name: "IsPlatformUserEnabled",
			table: "AspNetUsers",
			type: "bit",
			nullable: false,
			defaultValue: false);

		foreach (var tableName in new[]
		{
			"PostalAddresses",
			"People",
			"PersonPhones",
			"PersonEmails",
			"Locations",
			"LocationPhones",
			"LocationEmails",
			"Areas",
			"AreaCounties",
			"Goals",
			"SurveyDefinitions",
			"SurveyVersions",
			"SurveySections",
			"SurveyQuestions",
			"QuestionOptions",
			"SurveyAssignments",
			"SurveyResponses",
			"SurveyAnswers"
		})
		{
			migrationBuilder.AddColumn<int>(
				name: "TenantId",
				table: tableName,
				type: "int",
				nullable: false,
				defaultValue: 0);
		}

		migrationBuilder.CreateTable(
			name: "AuditLogs",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantId = table.Column<int>(type: "int", nullable: true),
				ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
				Plane = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
				ActionType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
				TargetType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
				TargetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
				Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
				Succeeded = table.Column<bool>(type: "bit", nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AuditLogs", item => item.Id);
			});

		migrationBuilder.CreateTable(
			name: "Tenants",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
				Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
				UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Tenants", item => item.Id);
			});

		migrationBuilder.CreateTable(
			name: "PlatformUserPermissions",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
				PermissionKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlatformUserPermissions", item => item.Id);
				table.ForeignKey(
					name: "FK_PlatformUserPermissions_AspNetUsers_UserId",
					column: item => item.UserId,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "TenantInvitations",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantId = table.Column<int>(type: "int", nullable: false),
				Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
				Role = table.Column<int>(type: "int", nullable: false),
				TokenHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
				ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
				CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
				AcceptedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
				RevokedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TenantInvitations", item => item.Id);
				table.ForeignKey(
					name: "FK_TenantInvitations_Tenants_TenantId",
					column: item => item.TenantId,
					principalTable: "Tenants",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "TenantMemberships",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantId = table.Column<int>(type: "int", nullable: false),
				UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
				Role = table.Column<int>(type: "int", nullable: false),
				IsEnabled = table.Column<bool>(type: "bit", nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
				UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TenantMemberships", item => item.Id);
				table.ForeignKey(
					name: "FK_TenantMemberships_AspNetUsers_UserId",
					column: item => item.UserId,
					principalTable: "AspNetUsers",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_TenantMemberships_Tenants_TenantId",
					column: item => item.TenantId,
					principalTable: "Tenants",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "TenantSettings",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantId = table.Column<int>(type: "int", nullable: false),
				ThemePresetKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
				UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TenantSettings", item => item.Id);
				table.ForeignKey(
					name: "FK_TenantSettings_Tenants_TenantId",
					column: item => item.TenantId,
					principalTable: "Tenants",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "TenantMembershipPermissions",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantMembershipId = table.Column<int>(type: "int", nullable: false),
				PermissionKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
				GrantKind = table.Column<int>(type: "int", nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TenantMembershipPermissions", item => item.Id);
				table.ForeignKey(
					name: "FK_TenantMembershipPermissions_TenantMemberships_TenantMembershipId",
					column: item => item.TenantMembershipId,
					principalTable: "TenantMemberships",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.DropIndex(
			name: "IX_PostalAddresses_NormalizedKey",
			table: "PostalAddresses");
		migrationBuilder.DropIndex(
			name: "IX_AreaCounties_AreaId_CountyFips",
			table: "AreaCounties");
		migrationBuilder.DropIndex(
			name: "IX_SurveyVersions_SurveyDefinitionId_VersionNumber",
			table: "SurveyVersions");
		migrationBuilder.DropIndex(
			name: "IX_People_Email",
			table: "People");

		migrationBuilder.CreateIndex(
			name: "IX_AuditLogs_TenantId_CreatedUtc",
			table: "AuditLogs",
			columns: new[] { "TenantId", "CreatedUtc" });
		migrationBuilder.CreateIndex(
			name: "IX_Tenants_Slug",
			table: "Tenants",
			column: "Slug",
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_PlatformUserPermissions_UserId_PermissionKey",
			table: "PlatformUserPermissions",
			columns: new[] { "UserId", "PermissionKey" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_TenantInvitations_TenantId_Email",
			table: "TenantInvitations",
			columns: new[] { "TenantId", "Email" });
		migrationBuilder.CreateIndex(
			name: "IX_TenantInvitations_TokenHash",
			table: "TenantInvitations",
			column: "TokenHash",
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_TenantMemberships_TenantId_UserId",
			table: "TenantMemberships",
			columns: new[] { "TenantId", "UserId" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_TenantMemberships_UserId",
			table: "TenantMemberships",
			column: "UserId");
		migrationBuilder.CreateIndex(
			name: "IX_TenantMembershipPermissions_TenantMembershipId_PermissionKey",
			table: "TenantMembershipPermissions",
			columns: new[] { "TenantMembershipId", "PermissionKey" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_TenantSettings_TenantId",
			table: "TenantSettings",
			column: "TenantId",
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_PostalAddresses_TenantId_NormalizedKey",
			table: "PostalAddresses",
			columns: new[] { "TenantId", "NormalizedKey" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_People_TenantId_Email",
			table: "People",
			columns: new[] { "TenantId", "Email" });
		migrationBuilder.CreateIndex(
			name: "IX_People_TenantId_IsArchived",
			table: "People",
			columns: new[] { "TenantId", "IsArchived" });
		migrationBuilder.CreateIndex(
			name: "IX_Locations_TenantId_PersonId",
			table: "Locations",
			columns: new[] { "TenantId", "PersonId" });
		migrationBuilder.CreateIndex(
			name: "IX_Areas_TenantId_Name",
			table: "Areas",
			columns: new[] { "TenantId", "Name" });
		migrationBuilder.CreateIndex(
			name: "IX_AreaCounties_TenantId_AreaId_CountyFips",
			table: "AreaCounties",
			columns: new[] { "TenantId", "AreaId", "CountyFips" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_Goals_TenantId_AreaId",
			table: "Goals",
			columns: new[] { "TenantId", "AreaId" });
		migrationBuilder.CreateIndex(
			name: "IX_SurveyDefinitions_TenantId_IsArchived",
			table: "SurveyDefinitions",
			columns: new[] { "TenantId", "IsArchived" });
		migrationBuilder.CreateIndex(
			name: "IX_SurveyVersions_TenantId_SurveyDefinitionId_VersionNumber",
			table: "SurveyVersions",
			columns: new[] { "TenantId", "SurveyDefinitionId", "VersionNumber" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_SurveyVersions_TenantId_IsArchived",
			table: "SurveyVersions",
			columns: new[] { "TenantId", "IsArchived" });
		migrationBuilder.CreateIndex(
			name: "IX_SurveyAssignments_TenantId_IsArchived",
			table: "SurveyAssignments",
			columns: new[] { "TenantId", "IsArchived" });
		migrationBuilder.CreateIndex(
			name: "IX_SurveyAssignments_TenantId_CreatedUtc",
			table: "SurveyAssignments",
			columns: new[] { "TenantId", "CreatedUtc" });
		migrationBuilder.CreateIndex(
			name: "IX_SurveyResponses_TenantId_SubmittedUtc",
			table: "SurveyResponses",
			columns: new[] { "TenantId", "SubmittedUtc" });
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropIndex(name: "IX_SurveyResponses_TenantId_SubmittedUtc", table: "SurveyResponses");
		migrationBuilder.DropIndex(name: "IX_SurveyAssignments_TenantId_CreatedUtc", table: "SurveyAssignments");
		migrationBuilder.DropIndex(name: "IX_SurveyAssignments_TenantId_IsArchived", table: "SurveyAssignments");
		migrationBuilder.DropIndex(name: "IX_SurveyVersions_TenantId_IsArchived", table: "SurveyVersions");
		migrationBuilder.DropIndex(name: "IX_SurveyVersions_TenantId_SurveyDefinitionId_VersionNumber", table: "SurveyVersions");
		migrationBuilder.DropIndex(name: "IX_SurveyDefinitions_TenantId_IsArchived", table: "SurveyDefinitions");
		migrationBuilder.DropIndex(name: "IX_Goals_TenantId_AreaId", table: "Goals");
		migrationBuilder.DropIndex(name: "IX_AreaCounties_TenantId_AreaId_CountyFips", table: "AreaCounties");
		migrationBuilder.DropIndex(name: "IX_Areas_TenantId_Name", table: "Areas");
		migrationBuilder.DropIndex(name: "IX_Locations_TenantId_PersonId", table: "Locations");
		migrationBuilder.DropIndex(name: "IX_People_TenantId_IsArchived", table: "People");
		migrationBuilder.DropIndex(name: "IX_People_TenantId_Email", table: "People");
		migrationBuilder.DropIndex(name: "IX_PostalAddresses_TenantId_NormalizedKey", table: "PostalAddresses");

		migrationBuilder.DropTable(name: "AuditLogs");
		migrationBuilder.DropTable(name: "PlatformUserPermissions");
		migrationBuilder.DropTable(name: "TenantInvitations");
		migrationBuilder.DropTable(name: "TenantMembershipPermissions");
		migrationBuilder.DropTable(name: "TenantSettings");
		migrationBuilder.DropTable(name: "TenantMemberships");
		migrationBuilder.DropTable(name: "Tenants");

		migrationBuilder.CreateIndex(
			name: "IX_PostalAddresses_NormalizedKey",
			table: "PostalAddresses",
			column: "NormalizedKey",
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_AreaCounties_AreaId_CountyFips",
			table: "AreaCounties",
			columns: new[] { "AreaId", "CountyFips" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_SurveyVersions_SurveyDefinitionId_VersionNumber",
			table: "SurveyVersions",
			columns: new[] { "SurveyDefinitionId", "VersionNumber" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_People_Email",
			table: "People",
			column: "Email");

		foreach (var tableName in new[]
		{
			"PostalAddresses",
			"People",
			"PersonPhones",
			"PersonEmails",
			"Locations",
			"LocationPhones",
			"LocationEmails",
			"Areas",
			"AreaCounties",
			"Goals",
			"SurveyDefinitions",
			"SurveyVersions",
			"SurveySections",
			"SurveyQuestions",
			"QuestionOptions",
			"SurveyAssignments",
			"SurveyResponses",
			"SurveyAnswers"
		})
		{
			migrationBuilder.DropColumn(name: "TenantId", table: tableName);
		}

		migrationBuilder.DropColumn(name: "ActiveTenantMembershipId", table: "AspNetUsers");
		migrationBuilder.DropColumn(name: "IsPlatformSuperAdmin", table: "AspNetUsers");
		migrationBuilder.DropColumn(name: "IsPlatformUserEnabled", table: "AspNetUsers");
	}
}
