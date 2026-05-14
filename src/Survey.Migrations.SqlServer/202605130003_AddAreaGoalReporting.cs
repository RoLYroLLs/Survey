using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130003_AddAreaGoalReporting")]
public class AddAreaGoalReporting : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [PostalCode] nvarchar(10) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentPostalCode] nvarchar(10) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentCountyFipsSnapshot] nvarchar(5) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentCountyNameSnapshot] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentStateCodeSnapshot] nvarchar(2) NULL;""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [Areas] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [Name] nvarchar(200) NOT NULL,
			    [Description] nvarchar(2000) NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [PK_Areas] PRIMARY KEY ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [ZipCountyLookups] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [ZipCode] nvarchar(10) NOT NULL,
			    [CountyFips] nvarchar(5) NOT NULL,
			    [CountyName] nvarchar(200) NOT NULL,
			    [StateCode] nvarchar(2) NOT NULL,
			    [ResidentialRatio] decimal(9,6) NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [PK_ZipCountyLookups] PRIMARY KEY ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AreaCounties] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [AreaId] int NOT NULL,
			    [CountyFips] nvarchar(5) NOT NULL,
			    [CountyName] nvarchar(200) NOT NULL,
			    [StateCode] nvarchar(2) NOT NULL,
			    CONSTRAINT [PK_AreaCounties] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_AreaCounties_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [Goals] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [Name] nvarchar(200) NOT NULL,
			    [Description] nvarchar(2000) NULL,
			    [AreaId] int NOT NULL,
			    [SurveyDefinitionId] int NULL,
			    [TargetResponseCount] int NOT NULL,
			    [StartDate] date NOT NULL,
			    [EndDate] date NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [PK_Goals] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_Goals_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE NO ACTION,
			    CONSTRAINT [FK_Goals_SurveyDefinitions_SurveyDefinitionId] FOREIGN KEY ([SurveyDefinitionId]) REFERENCES [SurveyDefinitions] ([Id]) ON DELETE NO ACTION
			);
			""");

		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_AreaCounties_AreaId_CountyFips] ON [AreaCounties] ([AreaId], [CountyFips]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Goals_AreaId] ON [Goals] ([AreaId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Goals_SurveyDefinitionId] ON [Goals] ([SurveyDefinitionId]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_ZipCountyLookups_ZipCode_CountyFips] ON [ZipCountyLookups] ([ZipCode], [CountyFips]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_ZipCountyLookups_ZipCode] ON [ZipCountyLookups] ([ZipCode]);""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [Goals];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AreaCounties];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [ZipCountyLookups];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [Areas];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentStateCodeSnapshot];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentCountyNameSnapshot];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentCountyFipsSnapshot];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentPostalCode];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [PostalCode];""");
	}
}
