using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605170002_AddMultiTenantFoundation")]
public class AddMultiTenantFoundation : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		// SQLite databases are repaired at startup after migrations run.
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
	}
}
