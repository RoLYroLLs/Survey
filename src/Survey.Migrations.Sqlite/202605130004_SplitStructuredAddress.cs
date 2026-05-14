using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130004_SplitStructuredAddress")]
public class SplitStructuredAddress : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "AddressLine1" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "AddressLine2" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "City" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "State" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentAddressLine1" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentAddressLine2" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentCity" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentState" TEXT NULL;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentState";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentCity";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentAddressLine2";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentAddressLine1";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "State";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "City";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "AddressLine2";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "AddressLine1";""");
	}
}
