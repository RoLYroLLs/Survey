using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.SqlServer;

public partial class AddPlatformThemes : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "PlatformThemes",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
				Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
				Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
				PrimaryColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
				AccentColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
				BackgroundColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
				CssVariablesBlock = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
				UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlatformThemes", x => x.Id);
			});

		migrationBuilder.CreateIndex(
			name: "IX_PlatformThemes_Key",
			table: "PlatformThemes",
			column: "Key",
			unique: true);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "PlatformThemes");
	}
}
