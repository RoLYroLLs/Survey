using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Survey.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserPasskeys",
                columns: table => new
                {
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserPasskeys", x => x.CredentialId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FavoriteGoalIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsOrganizationAccount = table.Column<bool>(type: "boolean", nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActiveTenantMembershipId = table.Column<int>(type: "integer", nullable: true),
                    IsPlatformSuperAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    IsPlatformUserEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBootstrapPlatformOwner = table.Column<bool>(type: "boolean", nullable: false),
                    AvatarColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Plane = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Iso2Code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Iso3Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformThemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CssVariablesBlock = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformThemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeedStates",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    AppliedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedStates", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ThemePresetKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InitialSetupCompletedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZipCountyLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CountyFips = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CountyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ResidentialRatio = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZipCountyLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AreaCounties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AreaId = table.Column<int>(type: "integer", nullable: false),
                    CountyFips = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CountyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaCounties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaCounties_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PermissionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformUserPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateProvinces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubdivisionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateProvinces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateProvinces_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AreaId = table.Column<int>(type: "integer", nullable: true),
                    SurveyDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    TargetResponseCount = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Goals_SurveyDefinitions_SurveyDefinitionId",
                        column: x => x.SurveyDefinitionId,
                        principalTable: "SurveyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    SurveyDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyVersions_SurveyDefinitions_SurveyDefinitionId",
                        column: x => x.SurveyDefinitionId,
                        principalTable: "SurveyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QueueName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    HangfireJobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    CurrentStageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentStageLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentItemMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StageStatesJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    MetadataJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "PlatformUserInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsPlatformUserEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPlatformSuperAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    PermissionKeysJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    TenantRole = table.Column<int>(type: "integer", nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUserInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformUserInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ThemePresetKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantVisibleCountries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantVisibleCountries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantVisibleCountries_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantVisibleCountries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Counties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateProvinceId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FipsCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Counties_StateProvinces_StateProvinceId",
                        column: x => x.StateProvinceId,
                        principalTable: "StateProvinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantVisibleStateProvinces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    StateProvinceId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantVisibleStateProvinces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantVisibleStateProvinces_StateProvinces_StateProvinceId",
                        column: x => x.StateProvinceId,
                        principalTable: "StateProvinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantVisibleStateProvinces_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveySections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    SurveyVersionId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveySections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveySections_SurveyVersions_SurveyVersionId",
                        column: x => x.SurveyVersionId,
                        principalTable: "SurveyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundOperationEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BackgroundOperationId = table.Column<int>(type: "integer", nullable: false),
                    StageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StageLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Processed = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundOperationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundOperationEvents_BackgroundOperations_BackgroundOp~",
                        column: x => x.BackgroundOperationId,
                        principalTable: "BackgroundOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboundEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BackgroundOperationId = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HtmlBody = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    TextBody = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    TrackingToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    FirstOpenedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastOpenedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpenCount = table.Column<int>(type: "integer", nullable: false),
                    FirstClickedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastClickedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClickCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "TenantMembershipPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantMembershipId = table.Column<int>(type: "integer", nullable: false),
                    PermissionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GrantKind = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMembershipPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMembershipPermissions_TenantMemberships_TenantMembers~",
                        column: x => x.TenantMembershipId,
                        principalTable: "TenantMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostalAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    StateProvinceId = table.Column<int>(type: "integer", nullable: true),
                    CountyId = table.Column<int>(type: "integer", nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FormattedAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostalAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostalAddresses_Counties_CountyId",
                        column: x => x.CountyId,
                        principalTable: "Counties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostalAddresses_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostalAddresses_StateProvinces_StateProvinceId",
                        column: x => x.StateProvinceId,
                        principalTable: "StateProvinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantVisibleCounties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CountyId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantVisibleCounties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantVisibleCounties_Counties_CountyId",
                        column: x => x.CountyId,
                        principalTable: "Counties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantVisibleCounties_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    SurveySectionId = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HelpText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestions_SurveySections_SurveySectionId",
                        column: x => x.SurveySectionId,
                        principalTable: "SurveySections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboundEmailAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OutboundEmailId = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StartedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OutboundEmailId = table.Column<int>(type: "integer", nullable: false),
                    LinkType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DestinationUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IpAddressHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalAddressId = table.Column<int>(type: "integer", nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HomeAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MailingPostalAddressId = table.Column<int>(type: "integer", nullable: true),
                    MailingAddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MailingAddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MailingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MailingState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MailingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MailingPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BestTimeToContact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PreferredContactMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.ForeignKey(
                        name: "FK_People_PostalAddresses_MailingPostalAddressId",
                        column: x => x.MailingPostalAddressId,
                        principalTable: "PostalAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_People_PostalAddresses_PostalAddressId",
                        column: x => x.PostalAddressId,
                        principalTable: "PostalAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    SurveyQuestionId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_SurveyQuestions_SurveyQuestionId",
                        column: x => x.SurveyQuestionId,
                        principalTable: "SurveyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PostalAddressId = table.Column<int>(type: "integer", nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HomeAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MailingPostalAddressId = table.Column<int>(type: "integer", nullable: true),
                    MailingAddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MailingAddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MailingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MailingState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MailingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MailingPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Locations_PostalAddresses_MailingPostalAddressId",
                        column: x => x.MailingPostalAddressId,
                        principalTable: "PostalAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_PostalAddresses_PostalAddressId",
                        column: x => x.PostalAddressId,
                        principalTable: "PostalAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonEmails_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonPhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonPhones_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationEmails_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationPhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationPhones_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    LocationPhoneId = table.Column<int>(type: "integer", nullable: true),
                    LocationEmailId = table.Column<int>(type: "integer", nullable: true),
                    SurveyVersionId = table.Column<int>(type: "integer", nullable: false),
                    PublicToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyAssignments_LocationEmails_LocationEmailId",
                        column: x => x.LocationEmailId,
                        principalTable: "LocationEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAssignments_LocationPhones_LocationPhoneId",
                        column: x => x.LocationPhoneId,
                        principalTable: "LocationPhones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAssignments_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAssignments_SurveyVersions_SurveyVersionId",
                        column: x => x.SurveyVersionId,
                        principalTable: "SurveyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    SurveyAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SubmittedByEmployee = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondentFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RespondentMiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespondentLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RespondentPostalAddressId = table.Column<int>(type: "integer", nullable: true),
                    RespondentAddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RespondentAddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RespondentCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespondentState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespondentHomeAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RespondentPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RespondentMailingPostalAddressId = table.Column<int>(type: "integer", nullable: true),
                    RespondentMailingAddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RespondentMailingAddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RespondentMailingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespondentMailingState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespondentMailingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RespondentMailingPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RespondentCountyFipsSnapshot = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    RespondentCountyNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RespondentStateCodeSnapshot = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    RespondentPhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RespondentPhoneLabel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RespondentBestTimeToContact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespondentPreferredContactMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RespondentEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RespondentEmailLabel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SurveyNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SurveyVersionNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyResponses_PostalAddresses_RespondentMailingPostalAddr~",
                        column: x => x.RespondentMailingPostalAddressId,
                        principalTable: "PostalAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyResponses_PostalAddresses_RespondentPostalAddressId",
                        column: x => x.RespondentPostalAddressId,
                        principalTable: "PostalAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyResponses_SurveyAssignments_SurveyAssignmentId",
                        column: x => x.SurveyAssignmentId,
                        principalTable: "SurveyAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    SurveyResponseId = table.Column<int>(type: "integer", nullable: false),
                    SurveyQuestionId = table.Column<int>(type: "integer", nullable: false),
                    QuestionPromptSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    QuestionType = table.Column<int>(type: "integer", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    YesNoValue = table.Column<bool>(type: "boolean", nullable: true),
                    SelectedOptionId = table.Column<int>(type: "integer", nullable: true),
                    SelectedOptionIdsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyAnswers_SurveyQuestions_SurveyQuestionId",
                        column: x => x.SurveyQuestionId,
                        principalTable: "SurveyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAnswers_SurveyResponses_SurveyResponseId",
                        column: x => x.SurveyResponseId,
                        principalTable: "SurveyResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AreaCounties_AreaId",
                table: "AreaCounties",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaCounties_TenantId",
                table: "AreaCounties",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaCounties_TenantId_AreaId_CountyFips",
                table: "AreaCounties",
                columns: new[] { "TenantId", "AreaId", "CountyFips" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_TenantId",
                table: "Areas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_TenantId_Name",
                table: "Areas",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserPasskeys_UserId",
                table: "AspNetUserPasskeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_CreatedUtc",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CreatedUtc" });

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
                name: "IX_Counties_StateProvinceId_FipsCode",
                table: "Counties",
                columns: new[] { "StateProvinceId", "FipsCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Counties_StateProvinceId_Name",
                table: "Counties",
                columns: new[] { "StateProvinceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Iso2Code",
                table: "Countries",
                column: "Iso2Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                table: "Countries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Goals_AreaId",
                table: "Goals",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_SurveyDefinitionId",
                table: "Goals",
                column: "SurveyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_TenantId",
                table: "Goals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_TenantId_AreaId",
                table: "Goals",
                columns: new[] { "TenantId", "AreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationEmails_LocationId",
                table: "LocationEmails",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationEmails_TenantId",
                table: "LocationEmails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPhones_LocationId",
                table: "LocationPhones",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPhones_TenantId",
                table: "LocationPhones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_MailingPostalAddressId",
                table: "Locations",
                column: "MailingPostalAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_PersonId",
                table: "Locations",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_PostalAddressId",
                table: "Locations",
                column: "PostalAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId",
                table: "Locations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_PersonId",
                table: "Locations",
                columns: new[] { "TenantId", "PersonId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_People_MailingPostalAddressId",
                table: "People",
                column: "MailingPostalAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_People_PostalAddressId",
                table: "People",
                column: "PostalAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_People_TenantId",
                table: "People",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_People_TenantId_Email",
                table: "People",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_People_TenantId_IsArchived",
                table: "People",
                columns: new[] { "TenantId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonEmails_PersonId",
                table: "PersonEmails",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonEmails_TenantId",
                table: "PersonEmails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonPhones_PersonId",
                table: "PersonPhones",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonPhones_TenantId",
                table: "PersonPhones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformThemes_Key",
                table: "PlatformThemes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserInvitations_Email",
                table: "PlatformUserInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserInvitations_TenantId",
                table: "PlatformUserInvitations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserInvitations_TokenHash",
                table: "PlatformUserInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserPermissions_UserId_PermissionKey",
                table: "PlatformUserPermissions",
                columns: new[] { "UserId", "PermissionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostalAddresses_CountryId",
                table: "PostalAddresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_PostalAddresses_CountyId",
                table: "PostalAddresses",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_PostalAddresses_StateProvinceId",
                table: "PostalAddresses",
                column: "StateProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_PostalAddresses_TenantId",
                table: "PostalAddresses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostalAddresses_TenantId_NormalizedKey",
                table: "PostalAddresses",
                columns: new[] { "TenantId", "NormalizedKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_SurveyQuestionId",
                table: "QuestionOptions",
                column: "SurveyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_TenantId",
                table: "QuestionOptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StateProvinces_CountryId_Code",
                table: "StateProvinces",
                columns: new[] { "CountryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateProvinces_CountryId_Name",
                table: "StateProvinces",
                columns: new[] { "CountryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswers_SurveyQuestionId",
                table: "SurveyAnswers",
                column: "SurveyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswers_SurveyResponseId",
                table: "SurveyAnswers",
                column: "SurveyResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswers_TenantId",
                table: "SurveyAnswers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_LocationEmailId",
                table: "SurveyAssignments",
                column: "LocationEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_LocationId",
                table: "SurveyAssignments",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_LocationPhoneId",
                table: "SurveyAssignments",
                column: "LocationPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_PublicToken",
                table: "SurveyAssignments",
                column: "PublicToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_SurveyVersionId",
                table: "SurveyAssignments",
                column: "SurveyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_TenantId",
                table: "SurveyAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_TenantId_CreatedUtc",
                table: "SurveyAssignments",
                columns: new[] { "TenantId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_TenantId_IsArchived",
                table: "SurveyAssignments",
                columns: new[] { "TenantId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyDefinitions_TenantId",
                table: "SurveyDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyDefinitions_TenantId_IsArchived",
                table: "SurveyDefinitions",
                columns: new[] { "TenantId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_SurveySectionId",
                table: "SurveyQuestions",
                column: "SurveySectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_TenantId",
                table: "SurveyQuestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_RespondentMailingPostalAddressId",
                table: "SurveyResponses",
                column: "RespondentMailingPostalAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_RespondentPostalAddressId",
                table: "SurveyResponses",
                column: "RespondentPostalAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyAssignmentId",
                table: "SurveyResponses",
                column: "SurveyAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_TenantId",
                table: "SurveyResponses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_TenantId_SubmittedUtc",
                table: "SurveyResponses",
                columns: new[] { "TenantId", "SubmittedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveySections_SurveyVersionId",
                table: "SurveySections",
                column: "SurveyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySections_TenantId",
                table: "SurveySections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyVersions_SurveyDefinitionId",
                table: "SurveyVersions",
                column: "SurveyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyVersions_TenantId",
                table: "SurveyVersions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyVersions_TenantId_IsArchived",
                table: "SurveyVersions",
                columns: new[] { "TenantId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyVersions_TenantId_SurveyDefinitionId_VersionNumber",
                table: "SurveyVersions",
                columns: new[] { "TenantId", "SurveyDefinitionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Email",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TokenHash",
                table: "TenantInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembershipPermissions_TenantMembershipId_PermissionKey",
                table: "TenantMembershipPermissions",
                columns: new[] { "TenantMembershipId", "PermissionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_UserId",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_UserId",
                table: "TenantMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleCounties_CountyId",
                table: "TenantVisibleCounties",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleCounties_TenantId",
                table: "TenantVisibleCounties",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleCounties_TenantId_CountyId",
                table: "TenantVisibleCounties",
                columns: new[] { "TenantId", "CountyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleCountries_CountryId",
                table: "TenantVisibleCountries",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleCountries_TenantId",
                table: "TenantVisibleCountries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleCountries_TenantId_CountryId",
                table: "TenantVisibleCountries",
                columns: new[] { "TenantId", "CountryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleStateProvinces_StateProvinceId",
                table: "TenantVisibleStateProvinces",
                column: "StateProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleStateProvinces_TenantId",
                table: "TenantVisibleStateProvinces",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantVisibleStateProvinces_TenantId_StateProvinceId",
                table: "TenantVisibleStateProvinces",
                columns: new[] { "TenantId", "StateProvinceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZipCountyLookups_ZipCode",
                table: "ZipCountyLookups",
                column: "ZipCode");

            migrationBuilder.CreateIndex(
                name: "IX_ZipCountyLookups_ZipCode_CountyFips",
                table: "ZipCountyLookups",
                columns: new[] { "ZipCode", "CountyFips" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaCounties");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserPasskeys");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BackgroundOperationEvents");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "OutboundEmailAttempts");

            migrationBuilder.DropTable(
                name: "OutboundEmailClickEvents");

            migrationBuilder.DropTable(
                name: "PersonEmails");

            migrationBuilder.DropTable(
                name: "PersonPhones");

            migrationBuilder.DropTable(
                name: "PlatformThemes");

            migrationBuilder.DropTable(
                name: "PlatformUserInvitations");

            migrationBuilder.DropTable(
                name: "PlatformUserPermissions");

            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "SeedStates");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "SurveyAnswers");

            migrationBuilder.DropTable(
                name: "TenantInvitations");

            migrationBuilder.DropTable(
                name: "TenantMembershipPermissions");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropTable(
                name: "TenantVisibleCounties");

            migrationBuilder.DropTable(
                name: "TenantVisibleCountries");

            migrationBuilder.DropTable(
                name: "TenantVisibleStateProvinces");

            migrationBuilder.DropTable(
                name: "ZipCountyLookups");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "OutboundEmails");

            migrationBuilder.DropTable(
                name: "SurveyQuestions");

            migrationBuilder.DropTable(
                name: "SurveyResponses");

            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "BackgroundOperations");

            migrationBuilder.DropTable(
                name: "SurveySections");

            migrationBuilder.DropTable(
                name: "SurveyAssignments");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "LocationEmails");

            migrationBuilder.DropTable(
                name: "LocationPhones");

            migrationBuilder.DropTable(
                name: "SurveyVersions");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "SurveyDefinitions");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "PostalAddresses");

            migrationBuilder.DropTable(
                name: "Counties");

            migrationBuilder.DropTable(
                name: "StateProvinces");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
