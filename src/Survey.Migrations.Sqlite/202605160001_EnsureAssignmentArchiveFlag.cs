using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605160001_EnsureAssignmentArchiveFlag")]
public class EnsureAssignmentArchiveFlag : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		// Existing SQLite databases are repaired at startup before migrations run.
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
	}
}
