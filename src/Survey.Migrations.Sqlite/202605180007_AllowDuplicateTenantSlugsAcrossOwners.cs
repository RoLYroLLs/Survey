using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.Sqlite;

public partial class AllowDuplicateTenantSlugsAcrossOwners : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropIndex(
			name: "IX_Tenants_Slug",
			table: "Tenants");

		migrationBuilder.CreateIndex(
			name: "IX_Tenants_Slug",
			table: "Tenants",
			column: "Slug");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropIndex(
			name: "IX_Tenants_Slug",
			table: "Tenants");

		migrationBuilder.CreateIndex(
			name: "IX_Tenants_Slug",
			table: "Tenants",
			column: "Slug",
			unique: true);
	}
}
