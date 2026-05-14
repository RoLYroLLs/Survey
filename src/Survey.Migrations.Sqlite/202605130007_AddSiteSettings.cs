using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130007_AddSiteSettings")]
public class AddSiteSettings : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			CREATE TABLE "SiteSettings" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SiteSettings" PRIMARY KEY,
			    "ThemePresetKey" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "SiteSettings" ("Id", "ThemePresetKey", "UpdatedUtc")
			VALUES (1, 'coastal-current', CURRENT_TIMESTAMP);
			""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SiteSettings";""");
	}
}
