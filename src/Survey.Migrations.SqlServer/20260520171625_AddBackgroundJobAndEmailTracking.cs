using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Survey.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobAndEmailTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QueueName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HangfireJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    CurrentStageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentStageLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentItemMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StageStatesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 32000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 32000, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundOperations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundOperationEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackgroundOperationId = table.Column<int>(type: "int", nullable: false),
                    StageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StageLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Processed = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundOperationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundOperationEvents_BackgroundOperations_BackgroundOperationId",
                        column: x => x.BackgroundOperationId,
                        principalTable: "BackgroundOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboundEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackgroundOperationId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", maxLength: 32000, nullable: false),
                    TextBody = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    TrackingToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    FirstOpenedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastOpenedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OpenCount = table.Column<int>(type: "int", nullable: false),
                    FirstClickedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastClickedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundEmails_BackgroundOperations_BackgroundOperationId",
                        column: x => x.BackgroundOperationId,
                        principalTable: "BackgroundOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OutboundEmails_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboundEmailAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundEmailId = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundEmailAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundEmailAttempts_OutboundEmails_OutboundEmailId",
                        column: x => x.OutboundEmailId,
                        principalTable: "OutboundEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboundEmailClickEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundEmailId = table.Column<int>(type: "int", nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IpAddressHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundEmailClickEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundEmailClickEvents_OutboundEmails_OutboundEmailId",
                        column: x => x.OutboundEmailId,
                        principalTable: "OutboundEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundOperationEvents_BackgroundOperationId_CreatedUtc",
                table: "BackgroundOperationEvents",
                columns: new[] { "BackgroundOperationId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundOperations_CreatedUtc",
                table: "BackgroundOperations",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundOperations_Kind_Status",
                table: "BackgroundOperations",
                columns: new[] { "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundOperations_TenantId",
                table: "BackgroundOperations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmailAttempts_OutboundEmailId_AttemptNumber",
                table: "OutboundEmailAttempts",
                columns: new[] { "OutboundEmailId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmailClickEvents_OutboundEmailId_OccurredUtc",
                table: "OutboundEmailClickEvents",
                columns: new[] { "OutboundEmailId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_BackgroundOperationId",
                table: "OutboundEmails",
                column: "BackgroundOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_Status_CreatedUtc",
                table: "OutboundEmails",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_TenantId",
                table: "OutboundEmails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_TrackingToken",
                table: "OutboundEmails",
                column: "TrackingToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundOperationEvents");

            migrationBuilder.DropTable(
                name: "OutboundEmailAttempts");

            migrationBuilder.DropTable(
                name: "OutboundEmailClickEvents");

            migrationBuilder.DropTable(
                name: "OutboundEmails");

            migrationBuilder.DropTable(
                name: "BackgroundOperations");
        }
    }
}
