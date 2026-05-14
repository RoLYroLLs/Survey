using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130005_AddNormalizedGeography")]
public class AddNormalizedGeography : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			CREATE TABLE [Countries] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_Countries] PRIMARY KEY,
			    [Name] nvarchar(200) NOT NULL,
			    [Iso2Code] nvarchar(2) NOT NULL,
			    [Iso3Code] nvarchar(3) NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [StateProvinces] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_StateProvinces] PRIMARY KEY,
			    [CountryId] int NOT NULL,
			    [Name] nvarchar(200) NOT NULL,
			    [Code] nvarchar(20) NOT NULL,
			    [SubdivisionType] nvarchar(50) NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [FK_StateProvinces_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [Counties] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_Counties] PRIMARY KEY,
			    [StateProvinceId] int NOT NULL,
			    [Name] nvarchar(200) NOT NULL,
			    [FipsCode] nvarchar(20) NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [FK_Counties_StateProvinces_StateProvinceId] FOREIGN KEY ([StateProvinceId]) REFERENCES [StateProvinces] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [PostalAddresses] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_PostalAddresses] PRIMARY KEY,
			    [CountryId] int NOT NULL,
			    [StateProvinceId] int NULL,
			    [CountyId] int NULL,
			    [AddressLine1] nvarchar(200) NOT NULL,
			    [AddressLine2] nvarchar(200) NULL,
			    [City] nvarchar(100) NOT NULL,
			    [PostalCode] nvarchar(20) NOT NULL,
			    [FormattedAddress] nvarchar(500) NOT NULL,
			    [NormalizedKey] nvarchar(1000) NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [FK_PostalAddresses_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]),
			    CONSTRAINT [FK_PostalAddresses_StateProvinces_StateProvinceId] FOREIGN KEY ([StateProvinceId]) REFERENCES [StateProvinces] ([Id]),
			    CONSTRAINT [FK_PostalAddresses_Counties_CountyId] FOREIGN KEY ([CountyId]) REFERENCES [Counties] ([Id])
			);
			""");

		migrationBuilder.Sql("""ALTER TABLE [People] ADD [PostalAddressId] int NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentPostalAddressId] int NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ALTER COLUMN [PostalCode] nvarchar(20) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ALTER COLUMN [RespondentPostalCode] nvarchar(20) NULL;""");

		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_Countries_Name] ON [Countries] ([Name]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_Countries_Iso2Code] ON [Countries] ([Iso2Code]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_StateProvinces_CountryId_Name] ON [StateProvinces] ([CountryId], [Name]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_StateProvinces_CountryId_Code] ON [StateProvinces] ([CountryId], [Code]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_StateProvinces_CountryId] ON [StateProvinces] ([CountryId]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_Counties_StateProvinceId_Name] ON [Counties] ([StateProvinceId], [Name]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_Counties_StateProvinceId_FipsCode] ON [Counties] ([StateProvinceId], [FipsCode]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Counties_StateProvinceId] ON [Counties] ([StateProvinceId]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_PostalAddresses_NormalizedKey] ON [PostalAddresses] ([NormalizedKey]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_PostalAddresses_CountryId] ON [PostalAddresses] ([CountryId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_PostalAddresses_StateProvinceId] ON [PostalAddresses] ([StateProvinceId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_PostalAddresses_CountyId] ON [PostalAddresses] ([CountyId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_People_PostalAddressId] ON [People] ([PostalAddressId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyResponses_RespondentPostalAddressId] ON [SurveyResponses] ([RespondentPostalAddressId]);""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD CONSTRAINT [FK_People_PostalAddresses_PostalAddressId] FOREIGN KEY ([PostalAddressId]) REFERENCES [PostalAddresses] ([Id]);""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD CONSTRAINT [FK_SurveyResponses_PostalAddresses_RespondentPostalAddressId] FOREIGN KEY ([RespondentPostalAddressId]) REFERENCES [PostalAddresses] ([Id]);""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP CONSTRAINT [FK_SurveyResponses_PostalAddresses_RespondentPostalAddressId];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP CONSTRAINT [FK_People_PostalAddresses_PostalAddressId];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_SurveyResponses_RespondentPostalAddressId] ON [SurveyResponses];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_People_PostalAddressId] ON [People];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_PostalAddresses_CountyId] ON [PostalAddresses];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_PostalAddresses_StateProvinceId] ON [PostalAddresses];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_PostalAddresses_CountryId] ON [PostalAddresses];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_PostalAddresses_NormalizedKey] ON [PostalAddresses];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Counties_StateProvinceId] ON [Counties];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Counties_StateProvinceId_FipsCode] ON [Counties];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Counties_StateProvinceId_Name] ON [Counties];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_StateProvinces_CountryId] ON [StateProvinces];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_StateProvinces_CountryId_Code] ON [StateProvinces];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_StateProvinces_CountryId_Name] ON [StateProvinces];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Countries_Iso2Code] ON [Countries];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Countries_Name] ON [Countries];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentPostalAddressId];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [PostalAddressId];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [PostalAddresses];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [Counties];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [StateProvinces];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [Countries];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ALTER COLUMN [RespondentPostalCode] nvarchar(10) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ALTER COLUMN [PostalCode] nvarchar(10) NULL;""");
	}
}
