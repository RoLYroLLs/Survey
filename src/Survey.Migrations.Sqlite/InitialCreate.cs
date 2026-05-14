using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130001_InitialCreate")]
public class InitialCreate : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetRoles" (
			    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
			    "Name" TEXT NULL,
			    "NormalizedName" TEXT NULL,
			    "ConcurrencyStamp" TEXT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetUsers" (
			    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
			    "FirstName" TEXT NULL,
			    "LastName" TEXT NULL,
			    "UserName" TEXT NULL,
			    "NormalizedUserName" TEXT NULL,
			    "Email" TEXT NULL,
			    "NormalizedEmail" TEXT NULL,
			    "EmailConfirmed" INTEGER NOT NULL,
			    "PasswordHash" TEXT NULL,
			    "SecurityStamp" TEXT NULL,
			    "ConcurrencyStamp" TEXT NULL,
			    "PhoneNumber" TEXT NULL,
			    "PhoneNumberConfirmed" INTEGER NOT NULL,
			    "TwoFactorEnabled" INTEGER NOT NULL,
			    "LockoutEnd" TEXT NULL,
			    "LockoutEnabled" INTEGER NOT NULL,
			    "AccessFailedCount" INTEGER NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "People" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_People" PRIMARY KEY AUTOINCREMENT,
			    "FirstName" TEXT NOT NULL,
			    "MiddleName" TEXT NULL,
			    "LastName" TEXT NOT NULL,
			    "HomeAddress" TEXT NOT NULL,
			    "PhoneNumber" TEXT NOT NULL,
			    "BestTimeToContact" TEXT NULL,
			    "Email" TEXT NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyDefinitions" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyDefinitions" PRIMARY KEY AUTOINCREMENT,
			    "Name" TEXT NOT NULL,
			    "Description" TEXT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetRoleClaims" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
			    "RoleId" TEXT NOT NULL,
			    "ClaimType" TEXT NULL,
			    "ClaimValue" TEXT NULL,
			    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetUserClaims" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
			    "UserId" TEXT NOT NULL,
			    "ClaimType" TEXT NULL,
			    "ClaimValue" TEXT NULL,
			    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetUserLogins" (
			    "LoginProvider" TEXT NOT NULL,
			    "ProviderKey" TEXT NOT NULL,
			    "ProviderDisplayName" TEXT NULL,
			    "UserId" TEXT NOT NULL,
			    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
			    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetUserPasskeys" (
			    "CredentialId" BLOB NOT NULL CONSTRAINT "PK_AspNetUserPasskeys" PRIMARY KEY,
			    "UserId" TEXT NOT NULL,
			    "Data" TEXT NOT NULL,
			    CONSTRAINT "FK_AspNetUserPasskeys_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetUserRoles" (
			    "UserId" TEXT NOT NULL,
			    "RoleId" TEXT NOT NULL,
			    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
			    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
			    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "AspNetUserTokens" (
			    "UserId" TEXT NOT NULL,
			    "LoginProvider" TEXT NOT NULL,
			    "Name" TEXT NOT NULL,
			    "Value" TEXT NULL,
			    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
			    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyVersions" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyVersions" PRIMARY KEY AUTOINCREMENT,
			    "SurveyDefinitionId" INTEGER NOT NULL,
			    "DisplayName" TEXT NOT NULL,
			    "VersionNumber" INTEGER NOT NULL,
			    "IsPublished" INTEGER NOT NULL,
			    "CreatedUtc" TEXT NOT NULL,
			    "UpdatedUtc" TEXT NOT NULL,
			    CONSTRAINT "FK_SurveyVersions_SurveyDefinitions_SurveyDefinitionId" FOREIGN KEY ("SurveyDefinitionId") REFERENCES "SurveyDefinitions" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveySections" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveySections" PRIMARY KEY AUTOINCREMENT,
			    "SurveyVersionId" INTEGER NOT NULL,
			    "Title" TEXT NOT NULL,
			    "Description" TEXT NULL,
			    "SortOrder" INTEGER NOT NULL,
			    CONSTRAINT "FK_SurveySections_SurveyVersions_SurveyVersionId" FOREIGN KEY ("SurveyVersionId") REFERENCES "SurveyVersions" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyAssignments" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyAssignments" PRIMARY KEY AUTOINCREMENT,
			    "PersonId" INTEGER NOT NULL,
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
			CREATE TABLE "SurveyQuestions" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyQuestions" PRIMARY KEY AUTOINCREMENT,
			    "SurveySectionId" INTEGER NOT NULL,
			    "Prompt" TEXT NOT NULL,
			    "HelpText" TEXT NULL,
			    "Type" INTEGER NOT NULL,
			    "IsRequired" INTEGER NOT NULL,
			    "SortOrder" INTEGER NOT NULL,
			    CONSTRAINT "FK_SurveyQuestions_SurveySections_SurveySectionId" FOREIGN KEY ("SurveySectionId") REFERENCES "SurveySections" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "QuestionOptions" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_QuestionOptions" PRIMARY KEY AUTOINCREMENT,
			    "SurveyQuestionId" INTEGER NOT NULL,
			    "Label" TEXT NOT NULL,
			    "SortOrder" INTEGER NOT NULL,
			    CONSTRAINT "FK_QuestionOptions_SurveyQuestions_SurveyQuestionId" FOREIGN KEY ("SurveyQuestionId") REFERENCES "SurveyQuestions" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyResponses" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyResponses" PRIMARY KEY AUTOINCREMENT,
			    "SurveyAssignmentId" INTEGER NOT NULL,
			    "SubmittedByUserId" TEXT NULL,
			    "SubmittedByEmployee" INTEGER NOT NULL,
			    "SubmittedUtc" TEXT NOT NULL,
			    "RespondentFirstName" TEXT NOT NULL,
			    "RespondentMiddleName" TEXT NULL,
			    "RespondentLastName" TEXT NOT NULL,
			    "RespondentHomeAddress" TEXT NOT NULL,
			    "RespondentPhoneNumber" TEXT NOT NULL,
			    "RespondentBestTimeToContact" TEXT NULL,
			    "RespondentEmail" TEXT NOT NULL,
			    "SurveyNameSnapshot" TEXT NOT NULL,
			    "SurveyVersionNameSnapshot" TEXT NOT NULL,
			    CONSTRAINT "FK_SurveyResponses_SurveyAssignments_SurveyAssignmentId" FOREIGN KEY ("SurveyAssignmentId") REFERENCES "SurveyAssignments" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql(
			"""
			CREATE TABLE "SurveyAnswers" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_SurveyAnswers" PRIMARY KEY AUTOINCREMENT,
			    "SurveyResponseId" INTEGER NOT NULL,
			    "SurveyQuestionId" INTEGER NOT NULL,
			    "QuestionPromptSnapshot" TEXT NOT NULL,
			    "QuestionType" INTEGER NOT NULL,
			    "AnswerText" TEXT NULL,
			    "YesNoValue" INTEGER NULL,
			    "SelectedOptionId" INTEGER NULL,
			    "SelectedOptionIdsJson" TEXT NULL,
			    CONSTRAINT "FK_SurveyAnswers_SurveyQuestions_SurveyQuestionId" FOREIGN KEY ("SurveyQuestionId") REFERENCES "SurveyQuestions" ("Id") ON DELETE RESTRICT,
			    CONSTRAINT "FK_SurveyAnswers_SurveyResponses_SurveyResponseId" FOREIGN KEY ("SurveyResponseId") REFERENCES "SurveyResponses" ("Id") ON DELETE CASCADE
			);
			""");

		migrationBuilder.Sql("""CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_AspNetUserPasskeys_UserId" ON "AspNetUserPasskeys" ("UserId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_People_Email" ON "People" ("Email");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_SurveyAssignments_PublicToken" ON "SurveyAssignments" ("PublicToken");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_PersonId" ON "SurveyAssignments" ("PersonId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAssignments_SurveyVersionId" ON "SurveyAssignments" ("SurveyVersionId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyQuestions_SurveySectionId" ON "SurveyQuestions" ("SurveySectionId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_QuestionOptions_SurveyQuestionId" ON "QuestionOptions" ("SurveyQuestionId");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_SurveyResponses_SurveyAssignmentId" ON "SurveyResponses" ("SurveyAssignmentId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAnswers_SurveyQuestionId" ON "SurveyAnswers" ("SurveyQuestionId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveyAnswers_SurveyResponseId" ON "SurveyAnswers" ("SurveyResponseId");""");
		migrationBuilder.Sql("""CREATE INDEX "IX_SurveySections_SurveyVersionId" ON "SurveySections" ("SurveyVersionId");""");
		migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_SurveyVersions_SurveyDefinitionId_VersionNumber" ON "SurveyVersions" ("SurveyDefinitionId", "VersionNumber");""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SurveyAnswers";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SurveyResponses";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "QuestionOptions";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetRoleClaims";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetUserClaims";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetUserLogins";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetUserPasskeys";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetUserRoles";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetUserTokens";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SurveyAssignments";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SurveyQuestions";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "People";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetRoles";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "AspNetUsers";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SurveySections";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SurveyVersions";""");
		migrationBuilder.Sql("""DROP TABLE IF EXISTS "SurveyDefinitions";""");
	}
}
