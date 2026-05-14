using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130002_AddArchiveFlags")]
public class AddArchiveFlags : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE "SurveyDefinitions" ADD COLUMN "IsArchived" INTEGER NOT NULL DEFAULT 0;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyVersions" ADD COLUMN "IsArchived" INTEGER NOT NULL DEFAULT 0;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE "SurveyDefinitions" DROP COLUMN "IsArchived";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyVersions" DROP COLUMN "IsArchived";""");
	}
}
