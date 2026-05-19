using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.SqlServer;

public partial class AddBootstrapPlatformOwnerFlag : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>(
			name: "IsBootstrapPlatformOwner",
			table: "AspNetUsers",
			type: "bit",
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
