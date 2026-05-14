using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605130006_AllowGoalsForAllAreas")]
public class AllowGoalsForAllAreas : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Goals_AreaId] ON [Goals];""");
		migrationBuilder.Sql("""ALTER TABLE [Goals] DROP CONSTRAINT [FK_Goals_Areas_AreaId];""");
		migrationBuilder.Sql("""ALTER TABLE [Goals] ALTER COLUMN [AreaId] int NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [Goals] ADD CONSTRAINT [FK_Goals_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE NO ACTION;""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Goals_AreaId] ON [Goals] ([AreaId]);""");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("""DROP INDEX IF EXISTS [IX_Goals_AreaId] ON [Goals];""");
		migrationBuilder.Sql("""ALTER TABLE [Goals] DROP CONSTRAINT [FK_Goals_Areas_AreaId];""");
		migrationBuilder.Sql("""DELETE FROM [Goals] WHERE [AreaId] IS NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [Goals] ALTER COLUMN [AreaId] int NOT NULL;""");
		migrationBuilder.Sql("""ALTER TABLE [Goals] ADD CONSTRAINT [FK_Goals_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE NO ACTION;""");
		migrationBuilder.Sql("""CREATE INDEX [IX_Goals_AreaId] ON [Goals] ([AreaId]);""");
	}
}
