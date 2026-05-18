using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140003_AddPreferredContactMethod")]
public class AddPreferredContactMethod : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [People] ADD [PreferredContactMethod] nvarchar(50) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] ADD [RespondentPreferredContactMethod] nvarchar(50) NULL;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [SurveyResponses] DROP COLUMN [RespondentPreferredContactMethod];""");
		migrationBuilder.Sql("""ALTER TABLE [People] DROP COLUMN [PreferredContactMethod];""");
	}
}
