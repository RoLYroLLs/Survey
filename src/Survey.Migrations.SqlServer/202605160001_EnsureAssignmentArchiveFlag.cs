using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605160001_EnsureAssignmentArchiveFlag")]
public class EnsureAssignmentArchiveFlag : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			IF COL_LENGTH('SurveyAssignments', 'IsArchived') IS NULL
			BEGIN
			    ALTER TABLE [SurveyAssignments] ADD [IsArchived] bit NOT NULL CONSTRAINT [DF_SurveyAssignments_IsArchived] DEFAULT 0;
			END
			""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			IF COL_LENGTH('SurveyAssignments', 'IsArchived') IS NOT NULL
			BEGIN
			    DECLARE @constraintName sysname;

			    SELECT @constraintName = default_constraints.name
			    FROM sys.columns columns
			    INNER JOIN sys.default_constraints default_constraints
			        ON default_constraints.parent_object_id = columns.object_id
			        AND default_constraints.parent_column_id = columns.column_id
			    WHERE columns.object_id = OBJECT_ID(N'[SurveyAssignments]')
			      AND columns.name = N'IsArchived';

			    IF @constraintName IS NOT NULL
			    BEGIN
			        EXEC(N'ALTER TABLE [SurveyAssignments] DROP CONSTRAINT [' + @constraintName + ']');
			    END

			    ALTER TABLE [SurveyAssignments] DROP COLUMN [IsArchived];
			END
			""");
	}
}
