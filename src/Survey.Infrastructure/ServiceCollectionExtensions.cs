using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Configuration;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Persistence;
using Survey.Infrastructure.Security;
using Survey.Infrastructure.Services;

namespace Survey.Infrastructure;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSurveyInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

		var databaseProvider = NormalizeProvider(configuration[$"{DatabaseOptions.SectionName}:Provider"]);
		var connectionString = configuration.GetConnectionString("Default")
			?? throw new InvalidOperationException("ConnectionStrings:Default must be configured.");
		var migrationsAssembly = databaseProvider == DatabaseOptions.SqlServer
			? "Survey.Migrations.SqlServer"
			: "Survey.Migrations.Sqlite";

		services.AddDbContext<SurveyDbContext>(options =>
		{
			if (databaseProvider == DatabaseOptions.SqlServer)
			{
				options.UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsAssembly(migrationsAssembly));
				return;
			}

			options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(migrationsAssembly));
		});

		services.AddIdentityCore<ApplicationUser>(options =>
			{
				options.SignIn.RequireConfirmedAccount = true;
				options.User.RequireUniqueEmail = true;
				options.Password.RequireDigit = true;
				options.Password.RequireUppercase = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireNonAlphanumeric = false;
			})
			.AddRoles<IdentityRole>()
			.AddEntityFrameworkStores<SurveyDbContext>()
			.AddSignInManager()
			.AddDefaultTokenProviders();

		services.AddHttpContextAccessor();
		services.AddScoped<TenantExecutionContext>();
		services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
		services.AddScoped<ITenantPermissionEvaluator, TenantPermissionEvaluator>();
		services.AddScoped<IPlatformPermissionEvaluator, PlatformPermissionEvaluator>();
		services.AddScoped<IAuditWriter, AuditWriter>();
		services.AddScoped<SurveyApplicationService>();
		services.AddScoped<ITenantAdministrationService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<IPlatformAdministrationService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<ISurveyExperienceService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<IdentityDataSeeder>();
		services.AddScoped<TenantBootstrapSeeder>();
		services.AddScoped<GeographyDataSeeder>();
		services.AddScoped<SiteSettingsSeeder>();
		services.AddScoped<IAuthorizationHandler, TenantPermissionAuthorizationHandler>();
		services.AddScoped<IAuthorizationHandler, PlatformPermissionAuthorizationHandler>();

		var authorization = services.AddAuthorizationBuilder();
		authorization.AddPolicy(SurveyAuthorizationPolicies.TenantAccess, policy =>
		{
			policy.RequireAuthenticatedUser();
			policy.AddRequirements(new TenantPermissionRequirement(null));
		});
		authorization.AddPolicy(SurveyAuthorizationPolicies.PlatformAccess, policy =>
		{
			policy.RequireAuthenticatedUser();
			policy.AddRequirements(new PlatformPermissionRequirement(null));
		});

		foreach (var permissionKey in TenantPermissionKeys.All)
		{
			authorization.AddPolicy(SurveyAuthorizationPolicies.TenantPermission(permissionKey), policy =>
			{
				policy.RequireAuthenticatedUser();
				policy.AddRequirements(new TenantPermissionRequirement(permissionKey));
			});
		}

		foreach (var permissionKey in PlatformPermissionKeys.All)
		{
			authorization.AddPolicy(SurveyAuthorizationPolicies.PlatformPermission(permissionKey), policy =>
			{
				policy.RequireAuthenticatedUser();
				policy.AddRequirements(new PlatformPermissionRequirement(permissionKey));
			});
		}

		return services;
	}

	public static async Task InitializeSurveyPlatformAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
	{
		using var scope = serviceProvider.CreateScope();
		serviceProvider = scope.ServiceProvider;
		var configuration = serviceProvider.GetRequiredService<IConfiguration>();
		var databaseProvider = NormalizeProvider(configuration[$"{DatabaseOptions.SectionName}:Provider"]);
		var connectionString = configuration.GetConnectionString("Default");

		if (databaseProvider == DatabaseOptions.Sqlite && !string.IsNullOrWhiteSpace(connectionString))
		{
			EnsureSqliteDirectoryExists(connectionString);
			await ClearStaleSqliteMigrationLockAsync(connectionString, cancellationToken);
			await RepairLegacySqliteSchemaAsync(connectionString, cancellationToken);
		}

		var dbContext = serviceProvider.GetRequiredService<SurveyDbContext>();
		await dbContext.Database.MigrateAsync(cancellationToken);

		if (databaseProvider == DatabaseOptions.Sqlite && !string.IsNullOrWhiteSpace(connectionString))
		{
			await RepairLegacySqliteSchemaAsync(connectionString, cancellationToken);
		}

		var geographySeeder = serviceProvider.GetRequiredService<GeographyDataSeeder>();
		var forceGeographySeed = configuration.GetValue<bool>("Seeding:ForceGeography");
		await geographySeeder.SeedAsync(forceGeographySeed, cancellationToken);

		var siteSettingsSeeder = serviceProvider.GetRequiredService<SiteSettingsSeeder>();
		await siteSettingsSeeder.SeedAsync(cancellationToken);

		var seeder = serviceProvider.GetRequiredService<IdentityDataSeeder>();
		await seeder.SeedAsync(cancellationToken);

		var tenantBootstrapSeeder = serviceProvider.GetRequiredService<TenantBootstrapSeeder>();
		await tenantBootstrapSeeder.SeedAsync(cancellationToken);
	}

	public static string NormalizeProvider(string? provider)
	{
		if (string.Equals(provider, DatabaseOptions.SqlServer, StringComparison.OrdinalIgnoreCase))
		{
			return DatabaseOptions.SqlServer;
		}

		return DatabaseOptions.Sqlite;
	}

	private static void EnsureSqliteDirectoryExists(string connectionString)
	{
		const string prefix = "Data Source=";
		var segment = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.FirstOrDefault(static part => part.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

		if (segment is null)
		{
			return;
		}

		var path = segment[(segment.IndexOf('=') + 1)..].Trim();
		if (Path.IsPathRooted(path))
		{
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			return;
		}

		var relativeDirectory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(relativeDirectory))
		{
			Directory.CreateDirectory(relativeDirectory);
		}
	}

	private static async Task ClearStaleSqliteMigrationLockAsync(string connectionString, CancellationToken cancellationToken)
	{
		await using var connection = new SqliteConnection(connectionString);
		await connection.OpenAsync(cancellationToken);

		await using var existsCommand = connection.CreateCommand();
		existsCommand.CommandText = """
			SELECT COUNT(*)
			FROM sqlite_master
			WHERE type = 'table' AND name = '__EFMigrationsLock';
			""";
		var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken)) > 0;
		if (!exists)
		{
			return;
		}

		await using var timestampCommand = connection.CreateCommand();
		timestampCommand.CommandText = """SELECT "Timestamp" FROM "__EFMigrationsLock" WHERE "Id" = 1;""";
		var timestampValue = await timestampCommand.ExecuteScalarAsync(cancellationToken) as string;
		if (string.IsNullOrWhiteSpace(timestampValue)
			|| !DateTimeOffset.TryParse(timestampValue, out var timestampUtc)
			|| DateTimeOffset.UtcNow - timestampUtc < TimeSpan.FromMinutes(1))
		{
			return;
		}

		await using var deleteCommand = connection.CreateCommand();
		deleteCommand.CommandText = """DELETE FROM "__EFMigrationsLock";""";
		await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
	}

	private static async Task RepairLegacySqliteSchemaAsync(string connectionString, CancellationToken cancellationToken)
	{
		await using var connection = new SqliteConnection(connectionString);
		await connection.OpenAsync(cancellationToken);

		await EnsureSqliteColumnExistsAsync(
			connection,
			"SurveyDefinitions",
			"IsArchived",
			"""ALTER TABLE "SurveyDefinitions" ADD COLUMN "IsArchived" INTEGER NOT NULL DEFAULT 0;""",
			cancellationToken);

		await EnsureSqliteColumnExistsAsync(
			connection,
			"SurveyVersions",
			"IsArchived",
			"""ALTER TABLE "SurveyVersions" ADD COLUMN "IsArchived" INTEGER NOT NULL DEFAULT 0;""",
			cancellationToken);

		await EnsureSqliteColumnExistsAsync(
			connection,
			"SurveyAssignments",
			"IsArchived",
			"""ALTER TABLE "SurveyAssignments" ADD COLUMN "IsArchived" INTEGER NOT NULL DEFAULT 0;""",
			cancellationToken);

		await EnsureSqliteColumnExistsAsync(
			connection,
			"People",
			"IsArchived",
			"""ALTER TABLE "People" ADD COLUMN "IsArchived" INTEGER NOT NULL DEFAULT 0;""",
			cancellationToken);

		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"AddressLine1",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "AddressLine1" TEXT NULL;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"AddressLine2",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "AddressLine2" TEXT NULL;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"City",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "City" TEXT NULL;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"State",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "State" TEXT NULL;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"PostalCode",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "PostalCode" TEXT NULL;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"ActiveTenantMembershipId",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "ActiveTenantMembershipId" INTEGER NULL;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"IsPlatformSuperAdmin",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "IsPlatformSuperAdmin" INTEGER NOT NULL DEFAULT 0;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"IsPlatformUserEnabled",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "IsPlatformUserEnabled" INTEGER NOT NULL DEFAULT 0;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"AvatarColorHex",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "AvatarColorHex" TEXT NULL;""",
			cancellationToken);

		foreach (var tableName in new[]
		{
			"PostalAddresses",
			"People",
			"PersonPhones",
			"PersonEmails",
			"Locations",
			"LocationPhones",
			"LocationEmails",
			"Areas",
			"AreaCounties",
			"Goals",
			"SurveyDefinitions",
			"SurveyVersions",
			"SurveySections",
			"SurveyQuestions",
			"QuestionOptions",
			"SurveyAssignments",
			"SurveyResponses",
			"SurveyAnswers"
		})
		{
			await EnsureSqliteColumnExistsAsync(
				connection,
				tableName,
				"TenantId",
				$"""ALTER TABLE "{tableName}" ADD COLUMN "TenantId" INTEGER NOT NULL DEFAULT 0;""",
				cancellationToken);
		}

		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "Tenants" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_Tenants" PRIMARY KEY AUTOINCREMENT,
				"Name" TEXT NOT NULL,
				"Slug" TEXT NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				"UpdatedUtc" TEXT NOT NULL
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "TenantMemberships" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_TenantMemberships" PRIMARY KEY AUTOINCREMENT,
				"TenantId" INTEGER NOT NULL,
				"UserId" TEXT NOT NULL,
				"Role" INTEGER NOT NULL,
				"IsEnabled" INTEGER NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				"UpdatedUtc" TEXT NOT NULL,
				CONSTRAINT "FK_TenantMemberships_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
				CONSTRAINT "FK_TenantMemberships_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "TenantMembershipPermissions" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_TenantMembershipPermissions" PRIMARY KEY AUTOINCREMENT,
				"TenantMembershipId" INTEGER NOT NULL,
				"PermissionKey" TEXT NOT NULL,
				"GrantKind" INTEGER NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				CONSTRAINT "FK_TenantMembershipPermissions_TenantMemberships_TenantMembershipId" FOREIGN KEY ("TenantMembershipId") REFERENCES "TenantMemberships" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "TenantInvitations" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_TenantInvitations" PRIMARY KEY AUTOINCREMENT,
				"TenantId" INTEGER NOT NULL,
				"Email" TEXT NOT NULL,
				"Role" INTEGER NOT NULL,
				"TokenHash" TEXT NOT NULL,
				"ExpiresAtUtc" TEXT NOT NULL,
				"CreatedByUserId" TEXT NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				"AcceptedUtc" TEXT NULL,
				"RevokedUtc" TEXT NULL,
				CONSTRAINT "FK_TenantInvitations_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "TenantSettings" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_TenantSettings" PRIMARY KEY AUTOINCREMENT,
				"TenantId" INTEGER NOT NULL,
				"ThemePresetKey" TEXT NOT NULL,
				"UpdatedUtc" TEXT NOT NULL,
				CONSTRAINT "FK_TenantSettings_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "TenantVisibleCountries" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_TenantVisibleCountries" PRIMARY KEY AUTOINCREMENT,
				"TenantId" INTEGER NOT NULL,
				"CountryId" INTEGER NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				CONSTRAINT "FK_TenantVisibleCountries_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
				CONSTRAINT "FK_TenantVisibleCountries_Countries_CountryId" FOREIGN KEY ("CountryId") REFERENCES "Countries" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "TenantVisibleStateProvinces" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_TenantVisibleStateProvinces" PRIMARY KEY AUTOINCREMENT,
				"TenantId" INTEGER NOT NULL,
				"StateProvinceId" INTEGER NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				CONSTRAINT "FK_TenantVisibleStateProvinces_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
				CONSTRAINT "FK_TenantVisibleStateProvinces_StateProvinces_StateProvinceId" FOREIGN KEY ("StateProvinceId") REFERENCES "StateProvinces" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "TenantVisibleCounties" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_TenantVisibleCounties" PRIMARY KEY AUTOINCREMENT,
				"TenantId" INTEGER NOT NULL,
				"CountyId" INTEGER NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				CONSTRAINT "FK_TenantVisibleCounties_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
				CONSTRAINT "FK_TenantVisibleCounties_Counties_CountyId" FOREIGN KEY ("CountyId") REFERENCES "Counties" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "PlatformUserPermissions" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_PlatformUserPermissions" PRIMARY KEY AUTOINCREMENT,
				"UserId" TEXT NOT NULL,
				"PermissionKey" TEXT NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				CONSTRAINT "FK_PlatformUserPermissions_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "AuditLogs" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY AUTOINCREMENT,
				"TenantId" INTEGER NULL,
				"ActorUserId" TEXT NULL,
				"Plane" TEXT NOT NULL,
				"ActionType" TEXT NOT NULL,
				"TargetType" TEXT NOT NULL,
				"TargetId" TEXT NULL,
				"Details" TEXT NULL,
				"Succeeded" INTEGER NOT NULL,
				"CreatedUtc" TEXT NOT NULL
			);
			""",
			cancellationToken);

		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tenants_Slug" ON "Tenants" ("Slug");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantMemberships_TenantId_UserId" ON "TenantMemberships" ("TenantId", "UserId");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_TenantMemberships_UserId" ON "TenantMemberships" ("UserId");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantMembershipPermissions_TenantMembershipId_PermissionKey" ON "TenantMembershipPermissions" ("TenantMembershipId", "PermissionKey");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantInvitations_TokenHash" ON "TenantInvitations" ("TokenHash");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_TenantInvitations_TenantId_Email" ON "TenantInvitations" ("TenantId", "Email");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantSettings_TenantId" ON "TenantSettings" ("TenantId");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantVisibleCountries_TenantId_CountryId" ON "TenantVisibleCountries" ("TenantId", "CountryId");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantVisibleStateProvinces_TenantId_StateProvinceId" ON "TenantVisibleStateProvinces" ("TenantId", "StateProvinceId");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantVisibleCounties_TenantId_CountyId" ON "TenantVisibleCounties" ("TenantId", "CountyId");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_PlatformUserPermissions_UserId_PermissionKey" ON "PlatformUserPermissions" ("UserId", "PermissionKey");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TenantId_CreatedUtc" ON "AuditLogs" ("TenantId", "CreatedUtc");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """DROP INDEX IF EXISTS "IX_PostalAddresses_NormalizedKey";""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_PostalAddresses_TenantId_NormalizedKey" ON "PostalAddresses" ("TenantId", "NormalizedKey");""", cancellationToken);
	}

	private static async Task EnsureSqliteColumnExistsAsync(
		SqliteConnection connection,
		string tableName,
		string columnName,
		string alterSql,
		CancellationToken cancellationToken)
	{
		if (!await SqliteTableExistsAsync(connection, tableName, cancellationToken))
		{
			return;
		}

		if (await SqliteColumnExistsAsync(connection, tableName, columnName, cancellationToken))
		{
			return;
		}

		await using var alterCommand = connection.CreateCommand();
		alterCommand.CommandText = alterSql;
		await alterCommand.ExecuteNonQueryAsync(cancellationToken);
	}

	private static async Task<bool> SqliteTableExistsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT COUNT(*)
			FROM sqlite_master
			WHERE type = 'table' AND name = $tableName;
			""";
		command.Parameters.AddWithValue("$tableName", tableName);
		return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
	}

	private static async Task<bool> SqliteColumnExistsAsync(SqliteConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = $"""PRAGMA table_info("{tableName}");""";

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static async Task ExecuteSqliteNonQueryAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		await command.ExecuteNonQueryAsync(cancellationToken);
	}
}
