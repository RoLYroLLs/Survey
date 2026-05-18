using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Survey.Infrastructure.Persistence;

namespace Survey.Migrations.SqlServer;

[DbContext(typeof(SurveyDbContext))]
[Migration("202605150001_AssignmentContactMethodOptional")]
public class AssignmentContactMethodOptional : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>(
			name: "IsArchived",
			table: "SurveyAssignments",
			type: "bit",
			nullable: false,
			defaultValue: false);

		migrationBuilder.AlterColumn<int>(
			name: "LocationPhoneId",
			table: "SurveyAssignments",
			type: "int",
			nullable: true,
			oldClrType: typeof(int),
			oldType: "int");

		migrationBuilder.AlterColumn<int>(
			name: "LocationEmailId",
			table: "SurveyAssignments",
			type: "int",
			nullable: true,
			oldClrType: typeof(int),
			oldType: "int");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql(
			"""
			UPDATE assignments
			SET [LocationPhoneId] = (
			    SELECT TOP 1 phones.[Id]
			    FROM [LocationPhones] phones
			    WHERE phones.[LocationId] = assignments.[LocationId]
			    ORDER BY phones.[SortOrder], phones.[Id])
			FROM [SurveyAssignments] assignments
			WHERE assignments.[LocationPhoneId] IS NULL;
			""");

		migrationBuilder.Sql(
			"""
			UPDATE assignments
			SET [LocationEmailId] = (
			    SELECT TOP 1 emails.[Id]
			    FROM [LocationEmails] emails
			    WHERE emails.[LocationId] = assignments.[LocationId]
			    ORDER BY emails.[SortOrder], emails.[Id])
			FROM [SurveyAssignments] assignments
			WHERE assignments.[LocationEmailId] IS NULL;
			""");

		migrationBuilder.Sql(
			"""
			IF EXISTS (
			    SELECT 1
			    FROM [SurveyAssignments]
			    WHERE [LocationPhoneId] IS NULL OR [LocationEmailId] IS NULL)
			BEGIN
			    THROW 51000, 'Cannot downgrade because one or more assignments do not have both a phone and an email contact.', 1;
			END
			""");

		migrationBuilder.AlterColumn<int>(
			name: "LocationPhoneId",
			table: "SurveyAssignments",
			type: "int",
			nullable: false,
			oldClrType: typeof(int),
			oldType: "int",
			oldNullable: true);

		migrationBuilder.AlterColumn<int>(
			name: "LocationEmailId",
			table: "SurveyAssignments",
			type: "int",
			nullable: false,
			oldClrType: typeof(int),
			oldType: "int",
			oldNullable: true);

		migrationBuilder.DropColumn(
			name: "IsArchived",
			table: "SurveyAssignments");
	}
}
