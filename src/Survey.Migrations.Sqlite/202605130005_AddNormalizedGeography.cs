using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130005_AddNormalizedGeography")]
public class AddNormalizedGeography : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			CREATE TABLE "Countries" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_Countries" PRIMARY KEY AUTOINCREMENT,
			    "Name" TEXT NOT NULL,
			    "Iso2Code" TEXT NOT NULL,
			    "Iso3Code" TEXT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "StateProvinces" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_StateProvinces" PRIMARY KEY AUTOINCREMENT,
			    "CountryId" INTEGER NOT NULL,
			    "Name" TEXT NOT NULL,
			    "Code" TEXT NOT NULL,
			    "SubdivisionType" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_StateProvinces_Countries_CountryId" FOREIGN KEY ("CountryId") REFERENCES "Countries" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "Counties" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_Counties" PRIMARY KEY AUTOINCREMENT,
			    "StateProvinceId" INTEGER NOT NULL,
			    "Name" TEXT NOT NULL,
			    "FipsCode" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_Counties_StateProvinces_StateProvinceId" FOREIGN KEY ("StateProvinceId") REFERENCES "StateProvinces" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "PostalAddresses" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_PostalAddresses" PRIMARY KEY AUTOINCREMENT,
			    "CountryId" INTEGER NOT NULL,
			    "StateProvinceId" INTEGER NULL,
			    "CountyId" INTEGER NULL,
			    "AddressLine1" TEXT NOT NULL,
			    "AddressLine2" TEXT NULL,
			    "City" TEXT NOT NULL,
			    "PostalCode" TEXT NOT NULL,
			    "FormattedAddress" TEXT NOT NULL,
			    "NormalizedKey" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_PostalAddresses_Countries_CountryId" FOREIGN KEY ("CountryId") REFERENCES "Countries" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_PostalAddresses_StateProvinces_StateProvinceId" FOREIGN KEY ("StateProvinceId") REFERENCES "StateProvinces" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_PostalAddresses_Counties_CountyId" FOREIGN KEY ("CountyId") REFERENCES "Counties" ("Id") ON DELETE RESTRICT
			);
			""");

		migrationBuilder.Sql("""ALTER TABLE "People" ADD COLUMN "PostalAddressId" INTEGER NULL;""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" ADD COLUMN "RespondentPostalAddressId" INTEGER NULL;""");

		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_Countries_Name" ON "Countries" ("Name");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_Countries_Iso2Code" ON "Countries" ("Iso2Code");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_StateProvinces_CountryId_Name" ON "StateProvinces" ("CountryId", "Name");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_StateProvinces_CountryId_Code" ON "StateProvinces" ("CountryId", "Code");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_StateProvinces_CountryId" ON "StateProvinces" ("CountryId");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_Counties_StateProvinceId_Name" ON "Counties" ("StateProvinceId", "Name");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_Counties_StateProvinceId_FipsCode" ON "Counties" ("StateProvinceId", "FipsCode");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_Counties_StateProvinceId" ON "Counties" ("StateProvinceId");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_PostalAddresses_NormalizedKey" ON "PostalAddresses" ("NormalizedKey");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_PostalAddresses_CountryId" ON "PostalAddresses" ("CountryId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_PostalAddresses_StateProvinceId" ON "PostalAddresses" ("StateProvinceId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_PostalAddresses_CountyId" ON "PostalAddresses" ("CountyId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_People_PostalAddressId" ON "People" ("PostalAddressId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyResponses_RespondentPostalAddressId" ON "SurveyResponses" ("RespondentPostalAddressId");""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SurveyResponses_RespondentPostalAddressId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_People_PostalAddressId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_PostalAddresses_CountyId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_PostalAddresses_StateProvinceId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_PostalAddresses_CountryId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_PostalAddresses_NormalizedKey";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Counties_StateProvinceId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Counties_StateProvinceId_FipsCode";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Counties_StateProvinceId_Name";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_StateProvinces_CountryId";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_StateProvinces_CountryId_Code";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_StateProvinces_CountryId_Name";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Countries_Iso2Code";""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Countries_Name";""");
		migrationBuilder.Sql("""ALTER TABLE "SurveyResponses" DROP COLUMN "RespondentPostalAddressId";""");
		migrationBuilder.Sql("""ALTER TABLE "People" DROP COLUMN "PostalAddressId";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "PostalAddresses";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "Counties";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "StateProvinces";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "Countries";""");
	}
}
