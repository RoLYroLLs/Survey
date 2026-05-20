using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.Sqlite.Migrations
{
	/// <inheritdoc />
	public partial class RequireInitialSetupAcknowledgement : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "InitialSetupCompletedUtc",
				table: "SiteSettings",
				type: "TEXT",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "InitialSetupCompletedUtc",
				table: "SiteSettings");
		}
	}
}
