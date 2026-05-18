using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140005_DropLegacyAssignmentPersonId")]
public class DropLegacyAssignmentPersonId : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""PRAGMA foreign_keys = OFF;""", suppressTransaction: true);

		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_PersonId";""");
		migrationBuilder.Sql(
			"""
			UPDATE "SurveyAssignments"
			SET "LocationPhoneId" = (
			    SELECT "Id"
			    FROM "LocationPhones"
			    WHERE "LocationPhones"."LocationId" = "SurveyAssignments"."LocationId"
			    ORDER BY "SortOrder", "Id"
			    LIMIT 1)
			WHERE "LocationId" IS NOT NULL
			  AND (
			      "LocationPhoneId" IS NULL
			      OR NOT EXISTS (
			          SELECT 1
			          FROM "LocationPhones"
			          WHERE "LocationPhones"."Id" = "SurveyAssignments"."LocationPhoneId"));
			""");
		migrationBuilder.Sql(
			"""
			UPDATE "SurveyAssignments"
			SET "LocationEmailId" = (
			    SELECT "Id"
			    FROM "LocationEmails"
			    WHERE "LocationEmails"."LocationId" = "SurveyAssignments"."LocationId"
			    ORDER BY "SortOrder", "Id"
			    LIMIT 1)
			WHERE "LocationId" IS NOT NULL
			  AND (
			      "LocationEmailId" IS NULL
			      OR NOT EXISTS (
			          SELECT 1
			          FROM "LocationEmails"
			          WHERE "LocationEmails"."Id" = "SurveyAssignments"."LocationEmailId"));
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyAssignments_Temp" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyAssignments" PRIMARY KEY AUTOINCREMENT,
			    "LocationId" INTEGER NOT NULL,
			    "LocationPhoneId" INTEGER NOT NULL,
			    "LocationEmailId" INTEGER NOT NULL,
			    "SurveyVersionId" INTEGER NOT NULL,
			    "PublicToken" TEXT NOT NULL,
			    "ExpiresAtUtc" TEXT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "CreatedByUserId" TEXT NULL,
			    CONSTRAINT "FK_SurveyAssignments_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_SurveyAssignments_LocationPhones_LocationPhoneId" FOREIGN KEY ("LocationPhoneId") REFERENCES "LocationPhones" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_SurveyAssignments_LocationEmails_LocationEmailId" FOREIGN KEY ("LocationEmailId") REFERENCES "LocationEmails" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_SurveyAssignments_SurveyVersions_SurveyVersionId" FOREIGN KEY ("SurveyVersionId") REFERENCES "SurveyVersions" ("Id") ON DELETE RESTRICT
			);
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "SurveyAssignments_Temp" (
			    "Id",
			    "LocationId",
			    "LocationPhoneId",
			    "LocationEmailId",
			    "SurveyVersionId",
			    "PublicToken",
			    "ExpiresAtUtc",
			    "CreatedUtc",
			    "CreatedByUserId")
			SELECT
			    "Id",
			    "LocationId",
			    "LocationPhoneId",
			    "LocationEmailId",
			    "SurveyVersionId",
			    "PublicToken",
			    "ExpiresAtUtc",
			    "CreatedUtc",
			    "CreatedByUserId"
			FROM "SurveyAssignments";
			""");

		migrationBuilder.Sql("""DROP TABLE "SurveyAssignments";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments_Temp" RENAME TO "SurveyAssignments";""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_SurveyAssignments_PublicToken" ON "SurveyAssignments" ("PublicToken");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_LocationId" ON "SurveyAssignments" ("LocationId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_LocationPhoneId" ON "SurveyAssignments" ("LocationPhoneId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_LocationEmailId" ON "SurveyAssignments" ("LocationEmailId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_SurveyVersionId" ON "SurveyAssignments" ("SurveyVersionId");""");
		migrationBuilder.Sql("""PRAGMA foreign_keys = ON;""", suppressTransaction: true);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""PRAGMA foreign_keys = OFF;""", suppressTransaction: true);

		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationEmailId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationPhoneId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_SurveyVersionId";""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyAssignments_Temp" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyAssignments" PRIMARY KEY AUTOINCREMENT,
			    "PersonId" INTEGER NOT NULL,
			    "LocationId" INTEGER NULL,
			    "LocationPhoneId" INTEGER NULL,
			    "LocationEmailId" INTEGER NULL,
			    "SurveyVersionId" INTEGER NOT NULL,
			    "PublicToken" TEXT NOT NULL,
			    "ExpiresAtUtc" TEXT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "CreatedByUserId" TEXT NULL,
			    CONSTRAINT "FK_SurveyAssignments_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_SurveyAssignments_SurveyVersions_SurveyVersionId" FOREIGN KEY ("SurveyVersionId") REFERENCES "SurveyVersions" ("Id") ON DELETE RESTRICT
			);
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "SurveyAssignments_Temp" (
			    "Id",
			    "PersonId",
			    "LocationId",
			    "LocationPhoneId",
			    "LocationEmailId",
			    "SurveyVersionId",
			    "PublicToken",
			    "ExpiresAtUtc",
			    "CreatedUtc",
			    "CreatedByUserId")
			SELECT
			    "Id",
			    (SELECT "PersonId" FROM "Locations" WHERE "Locations"."Id" = "SurveyAssignments"."LocationId"),
			    "LocationId",
			    "LocationPhoneId",
			    "LocationEmailId",
			    "SurveyVersionId",
			    "PublicToken",
			    "ExpiresAtUtc",
			    "CreatedUtc",
			    "CreatedByUserId"
			FROM "SurveyAssignments";
			""");

		migrationBuilder.Sql("""DROP TABLE "SurveyAssignments";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments_Temp" RENAME TO "SurveyAssignments";""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_SurveyAssignments_PublicToken" ON "SurveyAssignments" ("PublicToken");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_PersonId" ON "SurveyAssignments" ("PersonId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_SurveyVersionId" ON "SurveyAssignments" ("SurveyVersionId");""");
		migrationBuilder.Sql("""PRAGMA foreign_keys = ON;""", suppressTransaction: true);
	}
}
