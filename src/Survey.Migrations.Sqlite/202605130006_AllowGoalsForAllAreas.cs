using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130006_AllowGoalsForAllAreas")]
public class AllowGoalsForAllAreas : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""PRAGMA foreign_keys = 0;""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Goals_AreaId";""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "Goals_Temp" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_Goals_Temp" PRIMARY KEY AUTOINCREMENT,
			    "Name" TEXT NOT NULL,
			    "Description" TEXT NULL,
			    "AreaId" INTEGER NULL,
			    "SurveyDefinitionId" INTEGER NULL,
			    "TargetResponseCount" INTEGER NOT NULL,
			    "StartDate" TEXT NOT NULL,
			    "EndDate" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_Goals_Temp_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_Goals_Temp_SurveyDefinitions_SurveyDefinitionId" FOREIGN KEY ("SurveyDefinitionId") REFERENCES "SurveyDefinitions" ("Id") ON DELETE RESTRICT
			);
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "Goals_Temp" (
			    "Id",
			    "Name",
			    "Description",
			    "AreaId",
			    "SurveyDefinitionId",
			    "TargetResponseCount",
			    "StartDate",
			    "EndDate",
			    "CreatedUtc",
			    "UpdatedUtc")
			SELECT
			    "Id",
			    "Name",
			    "Description",
			    "AreaId",
			    "SurveyDefinitionId",
			    "TargetResponseCount",
			    "StartDate",
			    "EndDate",
			    "CreatedUtc",
			    "UpdatedUtc"
			FROM "Goals";
			""");

		migrationBuilder.Sql("""DROP TABLE "Goals";""");
		migrationBuilder.Sql("""ALTER TABLE "Goals_Temp" RENAME TO "Goals";""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Goals_AreaId" ON "Goals" ("AreaId");""");
		migrationBuilder.Sql("""PRAGMA foreign_keys = 1;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""PRAGMA foreign_keys = 0;""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Goals_AreaId";""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "Goals_Temp" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_Goals_Temp" PRIMARY KEY AUTOINCREMENT,
			    "Name" TEXT NOT NULL,
			    "Description" TEXT NULL,
			    "AreaId" INTEGER NOT NULL,
			    "SurveyDefinitionId" INTEGER NULL,
			    "TargetResponseCount" INTEGER NOT NULL,
			    "StartDate" TEXT NOT NULL,
			    "EndDate" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_Goals_Temp_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_Goals_Temp_SurveyDefinitions_SurveyDefinitionId" FOREIGN KEY ("SurveyDefinitionId") REFERENCES "SurveyDefinitions" ("Id") ON DELETE RESTRICT
			);
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "Goals_Temp" (
			    "Id",
			    "Name",
			    "Description",
			    "AreaId",
			    "SurveyDefinitionId",
			    "TargetResponseCount",
			    "StartDate",
			    "EndDate",
			    "CreatedUtc",
			    "UpdatedUtc")
			SELECT
			    "Id",
			    "Name",
			    "Description",
			    "AreaId",
			    "SurveyDefinitionId",
			    "TargetResponseCount",
			    "StartDate",
			    "EndDate",
			    "CreatedUtc",
			    "UpdatedUtc"
			FROM "Goals"
			WHERE "AreaId" IS NOT NULL;
			""");

		migrationBuilder.Sql("""DROP TABLE "Goals";""");
		migrationBuilder.Sql("""ALTER TABLE "Goals_Temp" RENAME TO "Goals";""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Goals_AreaId" ON "Goals" ("AreaId");""");
		migrationBuilder.Sql("""PRAGMA foreign_keys = 1;""");
	}
}
