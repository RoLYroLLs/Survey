using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130003_AddAreaGoalReporting")]
public class AddAreaGoalReporting : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "PostalCode" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentPostalCode" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentCountyFipsSnapshot" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentCountyNameSnapshot" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentStateCodeSnapshot" TEXT NULL;""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "Areas" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_Areas" PRIMARY KEY AUTOINCREMENT,
			    "Name" TEXT NOT NULL,
			    "Description" TEXT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "ZipCountyLookups" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_ZipCountyLookups" PRIMARY KEY AUTOINCREMENT,
			    "ZipCode" TEXT NOT NULL,
			    "CountyFips" TEXT NOT NULL,
			    "CountyName" TEXT NOT NULL,
			    "StateCode" TEXT NOT NULL,
			    "ResidentialRatio" REAL NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AreaCounties" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_AreaCounties" PRIMARY KEY AUTOINCREMENT,
			    "AreaId" INTEGER NOT NULL,
			    "CountyFips" TEXT NOT NULL,
			    "CountyName" TEXT NOT NULL,
			    "StateCode" TEXT NOT NULL,
			    CONSTRAINT "FK_AreaCounties_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "Goals" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_Goals" PRIMARY KEY AUTOINCREMENT,
			    "Name" TEXT NOT NULL,
			    "Description" TEXT NULL,
			    "AreaId" INTEGER NOT NULL,
			    "SurveyDefinitionId" INTEGER NULL,
			    "TargetResponseCount" INTEGER NOT NULL,
			    "StartDate" TEXT NOT NULL,
			    "EndDate" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_Goals_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_Goals_SurveyDefinitions_SurveyDefinitionId" FOREIGN KEY ("SurveyDefinitionId") REFERENCES "SurveyDefinitions" ("Id") ON DELETE RESTRICT
			);
			""");

		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_AreaCounties_AreaId_CountyFips" ON "AreaCounties" ("AreaId", "CountyFips");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Goals_AreaId" ON "Goals" ("AreaId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Goals_SurveyDefinitionId" ON "Goals" ("SurveyDefinitionId");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_ZipCountyLookups_ZipCode_CountyFips" ON "ZipCountyLookups" ("ZipCode", "CountyFips");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_ZipCountyLookups_ZipCode" ON "ZipCountyLookups" ("ZipCode");""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "Goals";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AreaCounties";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "ZipCountyLookups";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "Areas";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentStateCodeSnapshot";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentCountyNameSnapshot";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentCountyFipsSnapshot";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentPostalCode";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "PostalCode";""");
	}
}
