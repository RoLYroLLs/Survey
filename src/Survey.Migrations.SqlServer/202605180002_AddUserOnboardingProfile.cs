using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605180002_AddUserOnboardingProfile")]
public class AddUserOnboardingProfile : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] ADD [AddressLine1] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] ADD [AddressLine2] nvarchar(200) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] ADD [City] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] ADD [State] nvarchar(100) NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] ADD [PostalCode] nvarchar(20) NULL;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] DROP COLUMN [AddressLine1];""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] DROP COLUMN [AddressLine2];""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] DROP COLUMN [City];""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] DROP COLUMN [State];""");
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] DROP COLUMN [PostalCode];""");
	}
}
