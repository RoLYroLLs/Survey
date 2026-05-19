using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.Sqlite;

public partial class AddPlatformThemeLifecycle : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>(
			name: "IsArchived",
			table: "PlatformThemes",
			type: "INTEGER",
			nullable: false,
			defaultValue: false);

		migrationBuilder.AddColumn<bool>(
			name: "IsEnabled",
			table: "PlatformThemes",
			type: "INTEGER",
			nullable: false,
			defaultValue: true);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropColumn(
			name: "IsArchived",
			table: "PlatformThemes");

		migrationBuilder.DropColumn(
			name: "IsEnabled",
			table: "PlatformThemes");
	}
}
