using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130004_SplitStructuredAddress")]
public class SplitStructuredAddress : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [AddressLine1] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [AddressLine2] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [City] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [State] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentAddressLine1] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentAddressLine2] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentCity] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentState] nvarchar(100) NULL;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentState];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentCity];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentAddressLine2];""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentAddressLine1];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [State];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [City];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [AddressLine2];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [AddressLine1];""");
	}
}
