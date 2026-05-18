using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.Sqlite;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140004_AddSeedStates")]
public class AddSeedStates : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""
CREATE TABLE "SeedStates"
(
	"Key" TEXT NOT NULL CONSTRAINT "PK_SeedStates" PRIMARY KEY,
	"Version" INTEGER NOT NULL,
	"AppliedUtc" TEXT NOT NULL
);
""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE "SeedStates";""");
	}
}
