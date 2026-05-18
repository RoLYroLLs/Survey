using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140002_AddLocationsAndMultiContact")]
public class AddLocationsAndMultiContact : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [MailingPostalAddressId] int NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [MailingAddressLine1] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [MailingAddressLine2] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [MailingCity] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [MailingState] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [MailingAddress] nvarchar(500) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [MailingPostalCode] nvarchar(20) NULL;""");
		migrationBuilder.Sql("""CREATE INDEX [IX_People_MailingPostalAddressId] ON [People] ([MailingPostalAddressId]);""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD CONSTRAINT [FK_People_PostalAddresses_MailingPostalAddressId] FOREIGN KEY ([MailingPostalAddressId]) REFERENCES [PostalAddresses] ([Id]);""");

		migrationBuilder.Sql(
			"""
			UPDATE [People]
			SET
			    [MailingPostalAddressId] = [PostalAddressId],
			    [MailingAddressLine1] = [AddressLine1],
			    [MailingAddressLine2] = [AddressLine2],
			    [MailingCity] = [City],
			    [MailingState] = [State],
			    [MailingAddress] = [HomeAddress],
			    [MailingPostalCode] = [PostalCode]
			WHERE [MailingAddress] IS NULL;
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [PersonPhones] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_PersonPhones] PRIMARY KEY,
			    [PersonId] int NOT NULL,
			    [Label] nvarchar(50) NOT NULL,
			    [PhoneNumber] nvarchar(50) NOT NULL,
			    [SortOrder] int NOT NULL,
			    CONSTRAINT [FK_PersonPhones_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [PersonEmails] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_PersonEmails] PRIMARY KEY,
			    [PersonId] int NOT NULL,
			    [Label] nvarchar(50) NOT NULL,
			    [EmailAddress] nvarchar(256) NOT NULL,
			    [SortOrder] int NOT NULL,
			    CONSTRAINT [FK_PersonEmails_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [Locations] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_Locations] PRIMARY KEY,
			    [PersonId] int NOT NULL,
			    [Nickname] nvarchar(200) NOT NULL,
			    [PostalAddressId] int NULL,
			    [AddressLine1] nvarchar(200) NULL,
			    [AddressLine2] nvarchar(200) NULL,
			    [City] nvarchar(100) NULL,
			    [State] nvarchar(100) NULL,
			    [HomeAddress] nvarchar(500) NOT NULL,
			    [PostalCode] nvarchar(20) NULL,
			    [MailingPostalAddressId] int NULL,
			    [MailingAddressLine1] nvarchar(200) NULL,
			    [MailingAddressLine2] nvarchar(200) NULL,
			    [MailingCity] nvarchar(100) NULL,
			    [MailingState] nvarchar(100) NULL,
			    [MailingAddress] nvarchar(500) NOT NULL,
			    [MailingPostalCode] nvarchar(20) NULL,
			    [PhoneNumber] nvarchar(50) NOT NULL,
			    [Email] nvarchar(256) NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [FK_Locations_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE CASCADE,
			    CONSTRAINT [FK_Locations_PostalAddresses_PostalAddressId] FOREIGN KEY ([PostalAddressId]) REFERENCES [PostalAddresses] ([Id]),
			    CONSTRAINT [FK_Locations_PostalAddresses_MailingPostalAddressId] FOREIGN KEY ([MailingPostalAddressId]) REFERENCES [PostalAddresses] ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [LocationPhones] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_LocationPhones] PRIMARY KEY,
			    [LocationId] int NOT NULL,
			    [Label] nvarchar(50) NOT NULL,
			    [PhoneNumber] nvarchar(50) NOT NULL,
			    [SortOrder] int NOT NULL,
			    CONSTRAINT [FK_LocationPhones_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [LocationEmails] (
			    [Id] int NOT NULL IDENTITY(1,1) CONSTRAINT [PK_LocationEmails] PRIMARY KEY,
			    [LocationId] int NOT NULL,
			    [Label] nvarchar(50) NOT NULL,
			    [EmailAddress] nvarchar(256) NOT NULL,
			    [SortOrder] int NOT NULL,
			    CONSTRAINT [FK_LocationEmails_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql("""CREATE INDEX [IX_PersonPhones_PersonId] ON [PersonPhones] ([PersonId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_PersonEmails_PersonId] ON [PersonEmails] ([PersonId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Locations_PersonId] ON [Locations] ([PersonId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Locations_PostalAddressId] ON [Locations] ([PostalAddressId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Locations_MailingPostalAddressId] ON [Locations] ([MailingPostalAddressId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_LocationPhones_LocationId] ON [LocationPhones] ([LocationId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_LocationEmails_LocationId] ON [LocationEmails] ([LocationId]);""");

		migrationBuilder.Sql(
			"""
			INSERT INTO [PersonPhones] ([PersonId], [Label], [PhoneNumber], [SortOrder])
			SELECT [Id], N'Primary', [PhoneNumber], 0
			FROM [People]
			WHERE LTRIM(RTRIM(ISNULL([PhoneNumber], N''))) <> N'';
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO [PersonEmails] ([PersonId], [Label], [EmailAddress], [SortOrder])
			SELECT [Id], N'Primary', [Email], 0
			FROM [People]
			WHERE LTRIM(RTRIM(ISNULL([Email], N''))) <> N'';
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO [Locations] (
			    [PersonId],
			    [Nickname],
			    [PostalAddressId],
			    [AddressLine1],
			    [AddressLine2],
			    [City],
			    [State],
			    [HomeAddress],
			    [PostalCode],
			    [MailingPostalAddressId],
			    [MailingAddressLine1],
			    [MailingAddressLine2],
			    [MailingCity],
			    [MailingState],
			    [MailingAddress],
			    [MailingPostalCode],
			    [PhoneNumber],
			    [Email],
			    [CreatedUtc],
			    [UpdatedUtc])
			SELECT
			    [Id],
			    N'Imported Location',
			    [PostalAddressId],
			    [AddressLine1],
			    [AddressLine2],
			    [City],
			    [State],
			    [HomeAddress],
			    [PostalCode],
			    [MailingPostalAddressId],
			    [MailingAddressLine1],
			    [MailingAddressLine2],
			    [MailingCity],
			    [MailingState],
			    ISNULL(NULLIF(LTRIM(RTRIM([MailingAddress])), N''), [HomeAddress]),
			    ISNULL([MailingPostalCode], [PostalCode]),
			    [PhoneNumber],
			    [Email],
			    [CreatedUtc],
			    [UpdatedUtc]
			FROM [People];
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO [LocationPhones] ([LocationId], [Label], [PhoneNumber], [SortOrder])
			SELECT [Id], N'Primary', [PhoneNumber], 0
			FROM [Locations]
			WHERE LTRIM(RTRIM(ISNULL([PhoneNumber], N''))) <> N'';
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO [LocationEmails] ([LocationId], [Label], [EmailAddress], [SortOrder])
			SELECT [Id], N'Primary', [Email], 0
			FROM [Locations]
			WHERE LTRIM(RTRIM(ISNULL([Email], N''))) <> N'';
			""");

		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ADD [LocationId] int NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ADD [LocationPhoneId] int NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ADD [LocationEmailId] int NULL;""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAssignments_LocationId] ON [SurveyAssignments] ([LocationId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAssignments_LocationPhoneId] ON [SurveyAssignments] ([LocationPhoneId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAssignments_LocationEmailId] ON [SurveyAssignments] ([LocationEmailId]);""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ADD CONSTRAINT [FK_SurveyAssignments_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]);""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ADD CONSTRAINT [FK_SurveyAssignments_LocationPhones_LocationPhoneId] FOREIGN KEY ([LocationPhoneId]) REFERENCES [LocationPhones] ([Id]);""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ADD CONSTRAINT [FK_SurveyAssignments_LocationEmails_LocationEmailId] FOREIGN KEY ([LocationEmailId]) REFERENCES [LocationEmails] ([Id]);""");

		migrationBuilder.Sql(
			"""
			UPDATE assignments
			SET
			    [LocationId] = locations.[Id],
			    [LocationPhoneId] = phoneMap.[Id],
			    [LocationEmailId] = emailMap.[Id]
			FROM [SurveyAssignments] assignments
			INNER JOIN [Locations] locations
			    ON locations.[PersonId] = assignments.[PersonId]
			OUTER APPLY (
			    SELECT TOP (1) [Id]
			    FROM [LocationPhones]
			    WHERE [LocationId] = locations.[Id]
			    ORDER BY [SortOrder], [Id]
			) phoneMap
			OUTER APPLY (
			    SELECT TOP (1) [Id]
			    FROM [LocationEmails]
			    WHERE [LocationId] = locations.[Id]
			    ORDER BY [SortOrder], [Id]
			) emailMap;
			""");

		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentMailingPostalAddressId] int NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentMailingAddressLine1] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentMailingAddressLine2] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentMailingCity] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentMailingState] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentMailingAddress] nvarchar(500) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentMailingPostalCode] nvarchar(20) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentPhoneLabel] nvarchar(50) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentEmailLabel] nvarchar(50) NULL;""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyResponses_RespondentMailingPostalAddressId] ON [SurveyResponses] ([RespondentMailingPostalAddressId]);""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD CONSTRAINT [FK_SurveyResponses_PostalAddresses_RespondentMailingPostalAddressId] FOREIGN KEY ([RespondentMailingPostalAddressId]) REFERENCES [PostalAddresses] ([Id]);""");

		migrationBuilder.Sql(
			"""
			UPDATE [SurveyResponses]
			SET
			    [RespondentMailingPostalAddressId] = [RespondentPostalAddressId],
			    [RespondentMailingAddressLine1] = [RespondentAddressLine1],
			    [RespondentMailingAddressLine2] = [RespondentAddressLine2],
			    [RespondentMailingCity] = [RespondentCity],
			    [RespondentMailingState] = [RespondentState],
			    [RespondentMailingAddress] = [RespondentHomeAddress],
			    [RespondentMailingPostalCode] = [RespondentPostalCode],
			    [RespondentPhoneLabel] = N'Primary',
			    [RespondentEmailLabel] = N'Primary'
			WHERE [RespondentMailingAddress] IS NULL;
			""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP CONSTRAINT [FK_SurveyResponses_PostalAddresses_RespondentMailingPostalAddressId];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] DROP CONSTRAINT [FK_SurveyAssignments_LocationEmails_LocationEmailId];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] DROP CONSTRAINT [FK_SurveyAssignments_LocationPhones_LocationPhoneId];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] DROP CONSTRAINT [FK_SurveyAssignments_Locations_LocationId];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP CONSTRAINT [FK_People_PostalAddresses_MailingPostalAddressId];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_SurveyResponses_RespondentMailingPostalAddressId] ON [SurveyResponses];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_SurveyAssignments_LocationEmailId] ON [SurveyAssignments];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_SurveyAssignments_LocationPhoneId] ON [SurveyAssignments];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_SurveyAssignments_LocationId] ON [SurveyAssignments];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_LocationEmails_LocationId] ON [LocationEmails];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_LocationPhones_LocationId] ON [LocationPhones];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Locations_MailingPostalAddressId] ON [Locations];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Locations_PostalAddressId] ON [Locations];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Locations_PersonId] ON [Locations];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_PersonEmails_PersonId] ON [PersonEmails];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_PersonPhones_PersonId] ON [PersonPhones];""");
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_People_MailingPostalAddressId] ON [People];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentEmailLabel];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentPhoneLabel];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentMailingPostalCode];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentMailingAddress];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentMailingState];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentMailingCity];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentMailingAddressLine2];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentMailingAddressLine1];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentMailingPostalAddressId];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] DROP COLUMN [LocationEmailId];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] DROP COLUMN [LocationPhoneId];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] DROP COLUMN [LocationId];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [LocationEmails];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [LocationPhones];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [Locations];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [PersonEmails];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [PersonPhones];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [MailingPostalCode];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [MailingAddress];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [MailingState];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [MailingCity];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [MailingAddressLine2];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [MailingAddressLine1];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [MailingPostalAddressId];""");
	}
}
