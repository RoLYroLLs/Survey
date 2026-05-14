using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140001_AddFavoriteGoalIdsToUsers")]
public class AddFavoriteGoalIdsToUsers : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] ADD [FavoriteGoalIds] nvarchar(2000) NULL;""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""ALTER TABLE [AspNetUsers] DROP COLUMN [FavoriteGoalIds];""");
	}
}
