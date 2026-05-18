using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140002_AddLocationsAndMultiContact")]
public class AddLocationsAndMultiContact : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "MailingPostalAddressId" INTEGER NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "MailingAddressLine1" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "MailingAddressLine2" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "MailingCity" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "MailingState" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "MailingAddress" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "MailingPostalCode" TEXT NULL;""");
		migrationBuilder.Sql("""CREATE INDEX "IX_People_MailingPostalAddressId" ON "People" ("MailingPostalAddressId");""");

		migrationBuilder.Sql(
			"""
			UPDATE "People"
			SET
			    "MailingPostalAddressId" = "PostalAddressId",
			    "MailingAddressLine1" = "AddressLine1",
			    "MailingAddressLine2" = "AddressLine2",
			    "MailingCity" = "City",
			    "MailingState" = "State",
			    "MailingAddress" = "HomeAddress",
			    "MailingPostalCode" = "PostalCode"
			WHERE "MailingAddress" IS NULL;
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "PersonPhones" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_PersonPhones" PRIMARY KEY AUTOINCREMENT,
			    "PersonId" INTEGER NOT NULL,
			    "Label" TEXT NOT NULL,
			    "PhoneNumber" TEXT NOT NULL,
			    "SortOrder" INTEGER NOT NULL,
			    CONSTRAINT "FK_PersonPhones_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "PersonEmails" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_PersonEmails" PRIMARY KEY AUTOINCREMENT,
			    "PersonId" INTEGER NOT NULL,
			    "Label" TEXT NOT NULL,
			    "EmailAddress" TEXT NOT NULL,
			    "SortOrder" INTEGER NOT NULL,
			    CONSTRAINT "FK_PersonEmails_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "Locations" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_Locations" PRIMARY KEY AUTOINCREMENT,
			    "PersonId" INTEGER NOT NULL,
			    "Nickname" TEXT NOT NULL,
			    "PostalAddressId" INTEGER NULL,
			    "AddressLine1" TEXT NULL,
			    "AddressLine2" TEXT NULL,
			    "City" TEXT NULL,
			    "State" TEXT NULL,
			    "HomeAddress" TEXT NOT NULL,
			    "PostalCode" TEXT NULL,
			    "MailingPostalAddressId" INTEGER NULL,
			    "MailingAddressLine1" TEXT NULL,
			    "MailingAddressLine2" TEXT NULL,
			    "MailingCity" TEXT NULL,
			    "MailingState" TEXT NULL,
			    "MailingAddress" TEXT NOT NULL,
			    "MailingPostalCode" TEXT NULL,
			    "PhoneNumber" TEXT NOT NULL,
			    "Email" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_Locations_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("Id") ON DELETE CASCADE,
			    CONSTRAINT "FK_Locations_PostalAddresses_PostalAddressId" FOREIGN KEY ("PostalAddressId") REFERENCES "PostalAddresses" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_Locations_PostalAddresses_MailingPostalAddressId" FOREIGN KEY ("MailingPostalAddressId") REFERENCES "PostalAddresses" ("Id") ON DELETE RESTRICT
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "LocationPhones" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_LocationPhones" PRIMARY KEY AUTOINCREMENT,
			    "LocationId" INTEGER NOT NULL,
			    "Label" TEXT NOT NULL,
			    "PhoneNumber" TEXT NOT NULL,
			    "SortOrder" INTEGER NOT NULL,
			    CONSTRAINT "FK_LocationPhones_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "LocationEmails" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_LocationEmails" PRIMARY KEY AUTOINCREMENT,
			    "LocationId" INTEGER NOT NULL,
			    "Label" TEXT NOT NULL,
			    "EmailAddress" TEXT NOT NULL,
			    "SortOrder" INTEGER NOT NULL,
			    CONSTRAINT "FK_LocationEmails_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql("""CREATE INDEX "IX_PersonPhones_PersonId" ON "PersonPhones" ("PersonId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_PersonEmails_PersonId" ON "PersonEmails" ("PersonId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Locations_PersonId" ON "Locations" ("PersonId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Locations_PostalAddressId" ON "Locations" ("PostalAddressId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Locations_MailingPostalAddressId" ON "Locations" ("MailingPostalAddressId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_LocationPhones_LocationId" ON "LocationPhones" ("LocationId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_LocationEmails_LocationId" ON "LocationEmails" ("LocationId");""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "PersonPhones" ("PersonId", "Label", "PhoneNumber", "SortOrder")
			SELECT "Id", 'Primary', "PhoneNumber", 0
			FROM "People"
			WHERE IFNULL(TRIM("PhoneNumber"), '') <> '';
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "PersonEmails" ("PersonId", "Label", "EmailAddress", "SortOrder")
			SELECT "Id", 'Primary', "Email", 0
			FROM "People"
			WHERE IFNULL(TRIM("Email"), '') <> '';
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "Locations" (
			    "PersonId",
			    "Nickname",
			    "PostalAddressId",
			    "AddressLine1",
			    "AddressLine2",
			    "City",
			    "State",
			    "HomeAddress",
			    "PostalCode",
			    "MailingPostalAddressId",
			    "MailingAddressLine1",
			    "MailingAddressLine2",
			    "MailingCity",
			    "MailingState",
			    "MailingAddress",
			    "MailingPostalCode",
			    "PhoneNumber",
			    "Email",
			    "CreatedUtc",
			    "UpdatedUtc")
			SELECT
			    "Id",
			    'Imported Location',
			    "PostalAddressId",
			    "AddressLine1",
			    "AddressLine2",
			    "City",
			    "State",
			    "HomeAddress",
			    "PostalCode",
			    "MailingPostalAddressId",
			    "MailingAddressLine1",
			    "MailingAddressLine2",
			    "MailingCity",
			    "MailingState",
			    IFNULL(NULLIF(TRIM("MailingAddress"), ''), "HomeAddress"),
			    COALESCE("MailingPostalCode", "PostalCode"),
			    "PhoneNumber",
			    "Email",
			    "CreatedUtc",
			    "UpdatedUtc"
			FROM "People";
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "LocationPhones" ("LocationId", "Label", "PhoneNumber", "SortOrder")
			SELECT "Id", 'Primary', "PhoneNumber", 0
			FROM "Locations"
			WHERE IFNULL(TRIM("PhoneNumber"), '') <> '';
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "LocationEmails" ("LocationId", "Label", "EmailAddress", "SortOrder")
			SELECT "Id", 'Primary', "Email", 0
			FROM "Locations"
			WHERE IFNULL(TRIM("Email"), '') <> '';
			""");

		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments" ADD COLUMN "LocationId" INTEGER NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments" ADD COLUMN "LocationPhoneId" INTEGER NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments" ADD COLUMN "LocationEmailId" INTEGER NULL;""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_LocationId" ON "SurveyAssignments" ("LocationId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_LocationPhoneId" ON "SurveyAssignments" ("LocationPhoneId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_LocationEmailId" ON "SurveyAssignments" ("LocationEmailId");""");

		migrationBuilder.Sql(
			"""
			UPDATE "SurveyAssignments"
			SET
			    "LocationId" = (
			        SELECT "Id"
			        FROM "Locations"
			        WHERE "Locations"."PersonId" = "SurveyAssignments"."PersonId"
			        ORDER BY "Id"
			        LIMIT 1),
			    "LocationPhoneId" = (
			        SELECT "LocationPhones"."Id"
			        FROM "LocationPhones"
			        INNER JOIN "Locations" ON "Locations"."Id" = "LocationPhones"."LocationId"
			        WHERE "Locations"."PersonId" = "SurveyAssignments"."PersonId"
			        ORDER BY "LocationPhones"."SortOrder", "LocationPhones"."Id"
			        LIMIT 1),
			    "LocationEmailId" = (
			        SELECT "LocationEmails"."Id"
			        FROM "LocationEmails"
			        INNER JOIN "Locations" ON "Locations"."Id" = "LocationEmails"."LocationId"
			        WHERE "Locations"."PersonId" = "SurveyAssignments"."PersonId"
			        ORDER BY "LocationEmails"."SortOrder", "LocationEmails"."Id"
			        LIMIT 1);
			""");

		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentMailingPostalAddressId" INTEGER NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentMailingAddressLine1" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentMailingAddressLine2" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentMailingCity" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentMailingState" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentMailingAddress" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentMailingPostalCode" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentPhoneLabel" TEXT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentEmailLabel" TEXT NULL;""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyResponses_RespondentMailingPostalAddressId" ON "SurveyResponses" ("RespondentMailingPostalAddressId");""");

		migrationBuilder.Sql(
			"""
			UPDATE "SurveyResponses"
			SET
			    "RespondentMailingPostalAddressId" = "RespondentPostalAddressId",
			    "RespondentMailingAddressLine1" = "RespondentAddressLine1",
			    "RespondentMailingAddressLine2" = "RespondentAddressLine2",
			    "RespondentMailingCity" = "RespondentCity",
			    "RespondentMailingState" = "RespondentState",
			    "RespondentMailingAddress" = "RespondentHomeAddress",
			    "RespondentMailingPostalCode" = "RespondentPostalCode",
			    "RespondentPhoneLabel" = 'Primary',
			    "RespondentEmailLabel" = 'Primary'
			WHERE "RespondentMailingAddress" IS NULL;
			""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyResponses_RespondentMailingPostalAddressId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationEmailId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationPhoneId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyAssignments_LocationId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_LocationEmails_LocationId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_LocationPhones_LocationId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Locations_MailingPostalAddressId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Locations_PostalAddressId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Locations_PersonId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_PersonEmails_PersonId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_PersonPhones_PersonId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_People_MailingPostalAddressId";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentEmailLabel";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentPhoneLabel";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentMailingPostalCode";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentMailingAddress";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentMailingState";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentMailingCity";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentMailingAddressLine2";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentMailingAddressLine1";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentMailingPostalAddressId";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments" DROP COLUMN "LocationEmailId";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments" DROP COLUMN "LocationPhoneId";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyAssignments" DROP COLUMN "LocationId";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "LocationEmails";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "LocationPhones";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "Locations";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "PersonEmails";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "PersonPhones";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "MailingPostalCode";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "MailingAddress";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "MailingState";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "MailingCity";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "MailingAddressLine2";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "MailingAddressLine1";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "MailingPostalAddressId";""");
	}
}
