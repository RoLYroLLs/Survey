using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130001_InitialCreate")]
public class InitialCreate : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetRoles] (
			    [Id] nvarchar(450) NOT NULL,
			    [Name] nvarchar(256) NULL,
			    [NormalizedName] nvarchar(256) NULL,
			    [ConcurrencyStamp] nvarchar(max) NULL,
			    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetUsers] (
			    [Id] nvarchar(450) NOT NULL,
			    [FirstName] nvarchar(100) NULL,
			    [LastName] nvarchar(100) NULL,
			    [UserName] nvarchar(256) NULL,
			    [NormalizedUserName] nvarchar(256) NULL,
			    [Email] nvarchar(256) NULL,
			    [NormalizedEmail] nvarchar(256) NULL,
			    [EmailConfirmed] bit NOT NULL,
			    [PasswordHash] nvarchar(max) NULL,
			    [SecurityStamp] nvarchar(max) NULL,
			    [ConcurrencyStamp] nvarchar(max) NULL,
			    [PhoneNumber] nvarchar(max) NULL,
			    [PhoneNumberConfirmed] bit NOT NULL,
			    [TwoFactorEnabled] bit NOT NULL,
			    [LockoutEnd] datetimeoffset NULL,
			    [LockoutEnabled] bit NOT NULL,
			    [AccessFailedCount] int NOT NULL,
			    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [People] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [FirstName] nvarchar(100) NOT NULL,
			    [MiddleName] nvarchar(100) NULL,
			    [LastName] nvarchar(100) NOT NULL,
			    [HomeAddress] nvarchar(500) NOT NULL,
			    [PhoneNumber] nvarchar(50) NOT NULL,
			    [BestTimeToContact] nvarchar(100) NULL,
			    [Email] nvarchar(256) NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [PK_People] PRIMARY KEY ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [SurveyDefinitions] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [Name] nvarchar(200) NOT NULL,
			    [Description] nvarchar(2000) NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [PK_SurveyDefinitions] PRIMARY KEY ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetRoleClaims] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [RoleId] nvarchar(450) NOT NULL,
			    [ClaimType] nvarchar(max) NULL,
			    [ClaimValue] nvarchar(max) NULL,
			    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetUserClaims] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [UserId] nvarchar(450) NOT NULL,
			    [ClaimType] nvarchar(max) NULL,
			    [ClaimValue] nvarchar(max) NULL,
			    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetUserLogins] (
			    [LoginProvider] nvarchar(128) NOT NULL,
			    [ProviderKey] nvarchar(128) NOT NULL,
			    [ProviderDisplayName] nvarchar(max) NULL,
			    [UserId] nvarchar(450) NOT NULL,
			    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
			    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetUserPasskeys] (
			    [CredentialId] varbinary(1024) NOT NULL,
			    [UserId] nvarchar(450) NOT NULL,
			    [Data] nvarchar(max) NOT NULL,
			    CONSTRAINT [PK_AspNetUserPasskeys] PRIMARY KEY ([CredentialId]),
			    CONSTRAINT [FK_AspNetUserPasskeys_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetUserRoles] (
			    [UserId] nvarchar(450) NOT NULL,
			    [RoleId] nvarchar(450) NOT NULL,
			    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
			    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
			    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [AspNetUserTokens] (
			    [UserId] nvarchar(450) NOT NULL,
			    [LoginProvider] nvarchar(128) NOT NULL,
			    [Name] nvarchar(128) NOT NULL,
			    [Value] nvarchar(max) NULL,
			    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
			    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [SurveyVersions] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [SurveyDefinitionId] int NOT NULL,
			    [DisplayName] nvarchar(200) NOT NULL,
			    [VersionNumber] int NOT NULL,
			    [IsPublished] bit NOT NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [PK_SurveyVersions] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_SurveyVersions_SurveyDefinitions_SurveyDefinitionId] FOREIGN KEY ([SurveyDefinitionId]) REFERENCES [SurveyDefinitions] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [SurveySections] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [SurveyVersionId] int NOT NULL,
			    [Title] nvarchar(200) NOT NULL,
			    [Description] nvarchar(1000) NULL,
			    [SortOrder] int NOT NULL,
			    CONSTRAINT [PK_SurveySections] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_SurveySections_SurveyVersions_SurveyVersionId] FOREIGN KEY ([SurveyVersionId]) REFERENCES [SurveyVersions] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [SurveyAssignments] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [PersonId] int NOT NULL,
			    [SurveyVersionId] int NOT NULL,
			    [PublicToken] nvarchar(100) NOT NULL,
			    [ExpiresAtUtc] datetimeoffset NULL,
			    [CreatedUtc] datetimeoffset NOT NULL,
			    [CreatedByUserId] nvarchar(450) NULL,
			    CONSTRAINT [PK_SurveyAssignments] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_SurveyAssignments_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE NO ACTION,
			    CONSTRAINT [FK_SurveyAssignments_SurveyVersions_SurveyVersionId] FOREIGN KEY ([SurveyVersionId]) REFERENCES [SurveyVersions] ([Id]) ON DELETE NO ACTION
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [SurveyQuestions] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [SurveySectionId] int NOT NULL,
			    [Prompt] nvarchar(2000) NOT NULL,
			    [HelpText] nvarchar(1000) NULL,
			    [Type] int NOT NULL,
			    [IsRequired] bit NOT NULL,
			    [SortOrder] int NOT NULL,
			    CONSTRAINT [PK_SurveyQuestions] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_SurveyQuestions_SurveySections_SurveySectionId] FOREIGN KEY ([SurveySectionId]) REFERENCES [SurveySections] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [QuestionOptions] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [SurveyQuestionId] int NOT NULL,
			    [Label] nvarchar(200) NOT NULL,
			    [SortOrder] int NOT NULL,
			    CONSTRAINT [PK_QuestionOptions] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_QuestionOptions_SurveyQuestions_SurveyQuestionId] FOREIGN KEY ([SurveyQuestionId]) REFERENCES [SurveyQuestions] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [SurveyResponses] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [SurveyAssignmentId] int NOT NULL,
			    [SubmittedByUserId] nvarchar(450) NULL,
			    [SubmittedByEmployee] bit NOT NULL,
			    [SubmittedUtc] datetimeoffset NOT NULL,
			    [RespondentFirstName] nvarchar(100) NOT NULL,
			    [RespondentMiddleName] nvarchar(100) NULL,
			    [RespondentLastName] nvarchar(100) NOT NULL,
			    [RespondentHomeAddress] nvarchar(500) NOT NULL,
			    [RespondentPhoneNumber] nvarchar(50) NOT NULL,
			    [RespondentBestTimeToContact] nvarchar(100) NULL,
			    [RespondentEmail] nvarchar(256) NOT NULL,
			    [SurveyNameSnapshot] nvarchar(200) NOT NULL,
			    [SurveyVersionNameSnapshot] nvarchar(200) NOT NULL,
			    CONSTRAINT [PK_SurveyResponses] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_SurveyResponses_SurveyAssignments_SurveyAssignmentId] FOREIGN KEY ([SurveyAssignmentId]) REFERENCES [SurveyAssignments] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE [SurveyAnswers] (
			    [Id] int IDENTITY(1,1) NOT NULL,
			    [SurveyResponseId] int NOT NULL,
			    [SurveyQuestionId] int NOT NULL,
			    [QuestionPromptSnapshot] nvarchar(2000) NOT NULL,
			    [QuestionType] int NOT NULL,
			    [AnswerText] nvarchar(max) NULL,
			    [YesNoValue] bit NULL,
			    [SelectedOptionId] int NULL,
			    [SelectedOptionIdsJson] nvarchar(max) NULL,
			    CONSTRAINT [PK_SurveyAnswers] PRIMARY KEY ([Id]),
			    CONSTRAINT [FK_SurveyAnswers_SurveyQuestions_SurveyQuestionId] FOREIGN KEY ([SurveyQuestionId]) REFERENCES [SurveyQuestions] ([Id]) ON DELETE NO ACTION,
			    CONSTRAINT [FK_SurveyAnswers_SurveyResponses_SurveyResponseId] FOREIGN KEY ([SurveyResponseId]) REFERENCES [SurveyResponses] ([Id]) ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql("""CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;""");
		migrationBuilder.Sql("""CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_AspNetUserPasskeys_UserId] ON [AspNetUserPasskeys] ([UserId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_People_Email] ON [People] ([Email]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_SurveyAssignments_PublicToken] ON [SurveyAssignments] ([PublicToken]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAssignments_PersonId] ON [SurveyAssignments] ([PersonId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAssignments_SurveyVersionId] ON [SurveyAssignments] ([SurveyVersionId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyQuestions_SurveySectionId] ON [SurveyQuestions] ([SurveySectionId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_QuestionOptions_SurveyQuestionId] ON [QuestionOptions] ([SurveyQuestionId]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_SurveyResponses_SurveyAssignmentId] ON [SurveyResponses] ([SurveyAssignmentId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAnswers_SurveyQuestionId] ON [SurveyAnswers] ([SurveyQuestionId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAnswers_SurveyResponseId] ON [SurveyAnswers] ([SurveyResponseId]);""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveySections_SurveyVersionId] ON [SurveySections] ([SurveyVersionId]);""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX [IX_SurveyVersions_SurveyDefinitionId_VersionNumber] ON [SurveyVersions] ([SurveyDefinitionId], [VersionNumber]);""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SurveyAnswers];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SurveyResponses];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [QuestionOptions];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetRoleClaims];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetUserClaims];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetUserLogins];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetUserPasskeys];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetUserRoles];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetUserTokens];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SurveyAssignments];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SurveyQuestions];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [People];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetRoles];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [AspNetUsers];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SurveySections];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SurveyVersions];""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SurveyDefinitions];""");
	}
}
