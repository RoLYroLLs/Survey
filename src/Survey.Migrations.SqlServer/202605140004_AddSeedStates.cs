using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605140004_AddSeedStates")]
public class AddSeedStates : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""
CREATE TABLE [SeedStates]
(
	[Key] nvarchar(200) NOT NULL CONSTRAINT [PK_SeedStates] PRIMARY KEY,
	[Version] int NOT NULL,
	[AppliedUtc] datetimeoffset NOT NULL
);
""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE [SeedStates];""");
	}
}
