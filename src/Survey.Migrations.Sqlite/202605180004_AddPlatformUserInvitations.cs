using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.Sqlite;

public partial class AddPlatformUserInvitations : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "PlatformUserInvitations",
			columns: table => new
			{
				Id = table.Column<int>(type: "INTEGER", nullable: false)
					.Annotation("Sqlite:Autoincrement", true),
				Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
				IsPlatformUserEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
				IsPlatformSuperAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
				PermissionKeysJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
				TenantId = table.Column<int>(type: "INTEGER", nullable: true),
				TenantRole = table.Column<int>(type: "INTEGER", nullable: true),
				TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
				ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
				CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
				AcceptedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
				RevokedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlatformUserInvitations", x => x.Id);
				table.ForeignKey(
					name: "FK_PlatformUserInvitations_Tenants_TenantId",
					column: x => x.TenantId,
					principalTable: "Tenants",
					principalColumn: "Id",
					onDelete: ReferentialAction.Restrict);
			});

		migrationBuilder.CreateIndex(
			name: "IX_PlatformUserInvitations_Email",
			table: "PlatformUserInvitations",
			column: "Email");

		migrationBuilder.CreateIndex(
			name: "IX_PlatformUserInvitations_TenantId",
			table: "PlatformUserInvitations",
			column: "TenantId");

		migrationBuilder.CreateIndex(
			name: "IX_PlatformUserInvitations_TokenHash",
			table: "PlatformUserInvitations",
			column: "TokenHash",
			unique: true);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "PlatformUserInvitations");
	}
}
