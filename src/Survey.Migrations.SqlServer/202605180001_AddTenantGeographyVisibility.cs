using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605180001_AddTenantGeographyVisibility")]
public class AddTenantGeographyVisibility : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "TenantVisibleCountries",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantId = table.Column<int>(type: "int", nullable: false),
				CountryId = table.Column<int>(type: "int", nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TenantVisibleCountries", item => item.Id);
				table.ForeignKey(
					name: "FK_TenantVisibleCountries_Countries_CountryId",
					column: item => item.CountryId,
					principalTable: "Countries",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_TenantVisibleCountries_Tenants_TenantId",
					column: item => item.TenantId,
					principalTable: "Tenants",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "TenantVisibleCounties",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantId = table.Column<int>(type: "int", nullable: false),
				CountyId = table.Column<int>(type: "int", nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TenantVisibleCounties", item => item.Id);
				table.ForeignKey(
					name: "FK_TenantVisibleCounties_Counties_CountyId",
					column: item => item.CountyId,
					principalTable: "Counties",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_TenantVisibleCounties_Tenants_TenantId",
					column: item => item.TenantId,
					principalTable: "Tenants",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "TenantVisibleStateProvinces",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("SqlServer:Identity", "1, 1"),
				TenantId = table.Column<int>(type: "int", nullable: false),
				StateProvinceId = table.Column<int>(type: "int", nullable: false),
				CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_TenantVisibleStateProvinces", item => item.Id);
				table.ForeignKey(
					name: "FK_TenantVisibleStateProvinces_StateProvinces_StateProvinceId",
					column: item => item.StateProvinceId,
					principalTable: "StateProvinces",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_TenantVisibleStateProvinces_Tenants_TenantId",
					column: item => item.TenantId,
					principalTable: "Tenants",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_TenantVisibleCountries_TenantId_CountryId",
			table: "TenantVisibleCountries",
			columns: new[] { "TenantId", "CountryId" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_TenantVisibleCountries_CountryId",
			table: "TenantVisibleCountries",
			column: "CountryId");
		migrationBuilder.CreateIndex(
			name: "IX_TenantVisibleCounties_TenantId_CountyId",
			table: "TenantVisibleCounties",
			columns: new[] { "TenantId", "CountyId" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_TenantVisibleCounties_CountyId",
			table: "TenantVisibleCounties",
			column: "CountyId");
		migrationBuilder.CreateIndex(
			name: "IX_TenantVisibleStateProvinces_TenantId_StateProvinceId",
			table: "TenantVisibleStateProvinces",
			columns: new[] { "TenantId", "StateProvinceId" },
			unique: true);
		migrationBuilder.CreateIndex(
			name: "IX_TenantVisibleStateProvinces_StateProvinceId",
			table: "TenantVisibleStateProvinces",
			column: "StateProvinceId");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(name: "TenantVisibleCountries");
		migrationBuilder.DropTable(name: "TenantVisibleCounties");
		migrationBuilder.DropTable(name: "TenantVisibleStateProvinces");
	}
}
