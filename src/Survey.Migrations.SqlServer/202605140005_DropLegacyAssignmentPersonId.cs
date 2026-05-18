using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140005_DropLegacyAssignmentPersonId")]
public class DropLegacyAssignmentPersonId : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SurveyAssignments_PersonId' AND object_id = OBJECT_ID(N'[SurveyAssignments]'))
			BEGIN
			    DROP INDEX [IX_SurveyAssignments_PersonId] ON [SurveyAssignments];
			END
			""");

		migrationBuilder.Sql(
			"""
			DECLARE @constraintName nvarchar(200);
			SELECT @constraintName = fk.name
			FROM sys.foreign_keys fk
			WHERE fk.parent_object_id = OBJECT_ID(N'[SurveyAssignments]')
			  AND fk.name = N'FK_SurveyAssignments_People_PersonId';

			IF @constraintName IS NOT NULL
			BEGIN
			    EXEC(N'ALTER TABLE [SurveyAssignments] DROP CONSTRAINT [' + @constraintName + ']');
			END
			""");

		migrationBuilder.Sql(
			"""
			IF COL_LENGTH('SurveyAssignments', 'PersonId') IS NOT NULL
			BEGIN
			    ALTER TABLE [SurveyAssignments] DROP COLUMN [PersonId];
			END
			""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			IF COL_LENGTH('SurveyAssignments', 'PersonId') IS NULL
			BEGIN
			    ALTER TABLE [SurveyAssignments] ADD [PersonId] int NULL;
			END
			""");

		migrationBuilder.Sql(
			"""
			UPDATE assignments
			SET [PersonId] = locations.[PersonId]
			FROM [SurveyAssignments] assignments
			INNER JOIN [Locations] locations
			    ON locations.[Id] = assignments.[LocationId]
			WHERE assignments.[PersonId] IS NULL;
			""");

		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ALTER COLUMN [PersonId] int NOT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyAssignments] ADD CONSTRAINT [FK_SurveyAssignments_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE NO ACTION;""");
		migrationBuilder.Sql("""CREATE INDEX [IX_SurveyAssignments_PersonId] ON [SurveyAssignments] ([PersonId]);""");
	}
}
