using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.Sqlite;

public partial class AddBootstrapPlatformOwnerFlag : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>(
			name: "IsBootstrapPlatformOwner",
			table: "AspNetUsers",
			type: "INTEGER",
			nullable: false,
			defaultValue: false);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropColumn(
			name: "IsBootstrapPlatformOwner",
			table: "AspNetUsers");
	}
}
