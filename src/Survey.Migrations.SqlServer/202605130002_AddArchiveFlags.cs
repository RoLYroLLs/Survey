using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130002_AddArchiveFlags")]
public class AddArchiveFlags : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [SurveyDefinitions] ADD [IsArchived] bit NOT NULL CONSTRAINT [DF_SurveyDefinitions_IsArchived] DEFAULT 0;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyVersions] ADD [IsArchived] bit NOT NULL CONSTRAINT [DF_SurveyVersions_IsArchived] DEFAULT 0;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [SurveyDefinitions] DROP CONSTRAINT [DF_SurveyDefinitions_IsArchived];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyDefinitions] DROP COLUMN [IsArchived];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyVersions] DROP CONSTRAINT [DF_SurveyVersions_IsArchived];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyVersions] DROP COLUMN [IsArchived];""");
	}
}
