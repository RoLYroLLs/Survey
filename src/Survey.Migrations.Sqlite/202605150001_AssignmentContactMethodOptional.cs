using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605150001_AssignmentContactMethodOptional")]
public class AssignmentContactMethodOptional : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""PRAGMA foreign_keys = OFF;""", suppressTransaction: true);

		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationEmailId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationPhoneId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_SurveyVersionId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_PublicToken";""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyAssignments_Temp" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyAssignments" PRIMARY KEY AUTOINCREMENT,
			    "LocationId" INTEGER NOT NULL,
			    "LocationPhoneId" INTEGER NULL,
			    "LocationEmailId" INTEGER NULL,
			    "SurveyVersionId" INTEGER NOT NULL,
			    "PublicToken" TEXT NOT NULL,
			    "ExpiresAtUtc" TEXT NULL,
			    "IsArchived" INTEGER NOT NULL DEFAULT 0,
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
			    "IsArchived",
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
			    0,
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
		migrationBuilder.Sql(
			"""
			UPDATE "SurveyAssignments"
			SET "LocationPhoneId" = (
			    SELECT "Id"
			    FROM "LocationPhones"
			    WHERE "LocationPhones"."LocationId" = "SurveyAssignments"."LocationId"
			    ORDER BY "SortOrder", "Id"
			    LIMIT 1)
			WHERE "LocationPhoneId" IS NULL;
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
			WHERE "LocationEmailId" IS NULL;
			""");

		migrationBuilder.Sql("""PRAGMA foreign_keys = OFF;""", suppressTransaction: true);

		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationEmailId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationPhoneId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_SurveyVersionId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_PublicToken";""");

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
			    "IsArchived" INTEGER NOT NULL DEFAULT 0,
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
			    "IsArchived",
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
			    COALESCE("IsArchived", 0),
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
}
