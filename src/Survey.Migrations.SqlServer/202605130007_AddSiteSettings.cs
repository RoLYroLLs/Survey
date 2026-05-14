using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130007_AddSiteSettings")]
public class AddSiteSettings : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			CREATE TABLE [SiteSettings] (
			    [Id] int NOT NULL,
			    [ThemePresetKey] nvarchar(100) NOT NULL,
			    [UpdatedUtc] datetimeoffset NOT NULL,
			    CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
			);
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO [SiteSettings] ([Id], [ThemePresetKey], [UpdatedUtc])
			VALUES (1, N'coastal-current', SYSDATETIMEOFFSET());
			""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP TABLE IF EXISTS [SiteSettings];""");
	}
}
