using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.SqlServer.Migrations
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
				type: "datetimeoffset",
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
