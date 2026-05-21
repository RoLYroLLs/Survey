using System.Configuration;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Hangfire.MySql;
using Hangfire.PostgreSql;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Extensions;
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
	private static readonly object HangfireSqliteConfigurationLock = new();

	public static IServiceCollection AddSurveyInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddMemoryCache();
		services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
		services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
		services.Configure<BackgroundJobsOptions>(configuration.GetSection(BackgroundJobsOptions.SectionName));

		var databaseProvider = NormalizeProvider(configuration[$"{DatabaseOptions.SectionName}:Provider"]);
		var connectionString = configuration.GetConnectionString("Default")
			?? throw new InvalidOperationException("ConnectionStrings:Default must be configured.");
		var migrationsAssembly = GetMigrationsAssembly(databaseProvider);
		var backgroundJobsOptions = configuration.GetSection(BackgroundJobsOptions.SectionName).Get<BackgroundJobsOptions>() ?? new BackgroundJobsOptions();

		services.AddDbContext<SurveyDbContext>(options =>
		{
			ConfigureSurveyDbProvider(options, databaseProvider, connectionString, migrationsAssembly);
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
		services.AddScoped<IUserStore<ApplicationUser>, SurveyUserStore>();

		services.AddHttpContextAccessor();
		services.AddScoped<TenantExecutionContext>();
		services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
		services.AddScoped<ITenantPermissionEvaluator, TenantPermissionEvaluator>();
		services.AddScoped<IPlatformPermissionEvaluator, PlatformPermissionEvaluator>();
		services.AddScoped<IAuditWriter, AuditWriter>();
		services.AddScoped<BackgroundOperationsService>();
		services.AddScoped<IBackgroundOperationsService>(serviceProvider => serviceProvider.GetRequiredService<BackgroundOperationsService>());
		services.AddScoped<InitialSetupJobService>();
		services.AddScoped<IInitialSetupJobService>(serviceProvider => serviceProvider.GetRequiredService<InitialSetupJobService>());
		services.AddScoped<IQueuedEmailService, QueuedEmailService>();
		services.AddScoped<IEmailTrackingService, EmailTrackingService>();
		services.AddScoped<IPublicOriginResolver, PublicOriginResolver>();
		services.AddSingleton<IEmailTransport, NoOpEmailTransport>();
		services.AddSingleton<InitialSetupTaskQueue>();
		services.AddHostedService<InitialSetupBackgroundWorker>();
		services.AddScoped<InitialSetupBackgroundRunner>();
		services.AddScoped<EmailHangfireJobRunner>();
		services.AddScoped<SurveyApplicationService>();
		services.AddScoped<ITenantAdministrationService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<IPlatformAdministrationService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<ISurveyExperienceService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<IdentityDataSeeder>();
		services.AddScoped<TenantBootstrapSeeder>();
		services.AddScoped<GeographyDataSeeder>();
		services.AddScoped<SiteSettingsSeeder>();
		services.AddScoped<InitialSetupSeeder>();
		services.AddSingleton<InitialSetupStateService>();
		services.AddSingleton<InitialSeedingProgressService>();
		services.AddScoped<IAuthorizationHandler, TenantPermissionAuthorizationHandler>();
		services.AddScoped<IAuthorizationHandler, PlatformPermissionAuthorizationHandler>();

		if (backgroundJobsOptions.Enabled)
		{
			services.AddHangfire(hangfire =>
			{
				hangfire.UseSimpleAssemblyNameTypeSerializer();
				hangfire.UseRecommendedSerializerSettings();

				ConfigureHangfireStorage(hangfire, databaseProvider, connectionString);
			});
			services.AddHangfireServer(serverOptions =>
			{
				serverOptions.WorkerCount = Math.Max(1, backgroundJobsOptions.WorkerCount);
				serverOptions.Queues =
				[
					backgroundJobsOptions.SetupQueueName,
					backgroundJobsOptions.EmailQueueName,
					backgroundJobsOptions.DefaultQueueName
				];
			});
		}

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
			if (await ShouldRepairLegacySqliteSchemaAsync(connectionString, cancellationToken))
			{
				await RepairLegacySqliteSchemaAsync(connectionString, cancellationToken);
			}
		}

		var dbContext = serviceProvider.GetRequiredService<SurveyDbContext>();
		await dbContext.Database.MigrateAsync(cancellationToken);

		if (databaseProvider == DatabaseOptions.SqlServer && !string.IsNullOrWhiteSpace(connectionString))
		{
			await RepairLegacySqlServerSchemaAsync(connectionString, cancellationToken);
		}

		if (databaseProvider == DatabaseOptions.Sqlite && !string.IsNullOrWhiteSpace(connectionString))
		{
			await RepairLegacySqliteSchemaAsync(connectionString, cancellationToken);
		}

		var seeder = serviceProvider.GetRequiredService<IdentityDataSeeder>();
		await seeder.SeedAsync(cancellationToken: cancellationToken);

		var tenantBootstrapSeeder = serviceProvider.GetRequiredService<TenantBootstrapSeeder>();
		await tenantBootstrapSeeder.SeedAsync(cancellationToken);
	}

	public static string NormalizeProvider(string? provider)
	{
		if (string.Equals(provider, DatabaseOptions.MySql, StringComparison.OrdinalIgnoreCase))
		{
			return DatabaseOptions.MySql;
		}

		if (string.Equals(provider, DatabaseOptions.Postgres, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
		{
			return DatabaseOptions.Postgres;
		}

		if (string.Equals(provider, DatabaseOptions.SqlServer, StringComparison.OrdinalIgnoreCase))
		{
			return DatabaseOptions.SqlServer;
		}

		return DatabaseOptions.Sqlite;
	}

	private static string GetMigrationsAssembly(string databaseProvider)
	{
		return databaseProvider switch
		{
			DatabaseOptions.MySql => "Survey.Migrations.MySql",
			DatabaseOptions.Postgres => "Survey.Migrations.Postgres",
			DatabaseOptions.SqlServer => "Survey.Migrations.SqlServer",
			_ => "Survey.Migrations.Sqlite"
		};
	}

	private static void ConfigureSurveyDbProvider(
		DbContextOptionsBuilder options,
		string databaseProvider,
		string connectionString,
		string migrationsAssembly)
	{
		switch (databaseProvider)
		{
			case DatabaseOptions.MySql:
				options.UseMySQL(connectionString, mySql => mySql.MigrationsAssembly(migrationsAssembly));
				break;

			case DatabaseOptions.Postgres:
				options.UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly(migrationsAssembly));
				break;

			case DatabaseOptions.SqlServer:
				options.UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsAssembly(migrationsAssembly));
				break;

			default:
				options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(migrationsAssembly));
				break;
		}
	}

	private static void ConfigureHangfireStorage(IGlobalConfiguration hangfire, string databaseProvider, string connectionString)
	{
		switch (databaseProvider)
		{
			case DatabaseOptions.MySql:
				hangfire.UseStorage(new MySqlStorage(
					EnsureHangfireMySqlConnectionString(connectionString),
					new MySqlStorageOptions
					{
						PrepareSchemaIfNecessary = true
					}));
				break;

			case DatabaseOptions.Postgres:
				hangfire.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
				break;

			case DatabaseOptions.SqlServer:
				hangfire.UseSqlServerStorage(connectionString);
				break;

			default:
				var sqliteConnectionName = EnsureHangfireSqliteConnectionStringRegistered(connectionString);
				hangfire.UseSQLiteStorage(sqliteConnectionName, new SQLiteStorageOptions
				{
					PrepareSchemaIfNecessary = true
				});
				break;
		}
	}

	private static string EnsureHangfireMySqlConnectionString(string connectionString)
	{
		const string optionName = "Allow User Variables";
		var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
		var optionIndex = segments.FindIndex(static segment => segment.StartsWith($"{optionName}=", StringComparison.OrdinalIgnoreCase));
		if (optionIndex >= 0)
		{
			segments[optionIndex] = $"{optionName}=True";
			return string.Join(';', segments);
		}

		segments.Add($"{optionName}=True");
		return string.Join(';', segments);
	}

	private static string EnsureHangfireSqliteConnectionStringRegistered(string connectionString)
	{
		var normalizedConnectionString = connectionString.Trim();
		var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedConnectionString)))[..16];
		var connectionName = $"SurveyHangfireSqlite_{hash}";

		lock (HangfireSqliteConfigurationLock)
		{
			var connectionStrings = System.Configuration.ConfigurationManager.ConnectionStrings;
			if (connectionStrings[connectionName] is null)
			{
				EnsureConfigurationCollectionWritable(connectionStrings);
				connectionStrings.Add(new ConnectionStringSettings(connectionName, normalizedConnectionString));
			}
		}

		return connectionName;
	}

	private static void EnsureConfigurationCollectionWritable(ConfigurationElementCollection collection)
	{
		var field = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
			?? typeof(ConfigurationElementCollection).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
			?? typeof(ConfigurationElementCollection).GetField("_readOnly", BindingFlags.Instance | BindingFlags.NonPublic);
		field?.SetValue(collection, false);
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

	private static async Task<bool> ShouldRepairLegacySqliteSchemaAsync(string connectionString, CancellationToken cancellationToken)
	{
		await using var connection = new SqliteConnection(connectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT COUNT(*)
			FROM sqlite_master
			WHERE type = 'table'
				AND name NOT LIKE 'sqlite_%'
				AND name <> '__EFMigrationsHistory'
				AND name <> '__EFMigrationsLock';
			""";

		return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
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
			"IsOrganizationAccount",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "IsOrganizationAccount" INTEGER NOT NULL DEFAULT 0;""",
			cancellationToken);
		await EnsureSqliteColumnExistsAsync(
			connection,
			"AspNetUsers",
			"OrganizationName",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "OrganizationName" TEXT NULL;""",
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
			"IsBootstrapPlatformOwner",
			"""ALTER TABLE "AspNetUsers" ADD COLUMN "IsBootstrapPlatformOwner" INTEGER NOT NULL DEFAULT 0;""",
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
			CREATE TABLE IF NOT EXISTS "PlatformUserInvitations" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_PlatformUserInvitations" PRIMARY KEY AUTOINCREMENT,
				"Email" TEXT NOT NULL,
				"IsPlatformUserEnabled" INTEGER NOT NULL,
				"IsPlatformSuperAdmin" INTEGER NOT NULL,
				"PermissionKeysJson" TEXT NOT NULL,
				"TenantId" INTEGER NULL,
				"TenantRole" INTEGER NULL,
				"TokenHash" TEXT NOT NULL,
				"ExpiresAtUtc" TEXT NOT NULL,
				"CreatedByUserId" TEXT NOT NULL,
				"CreatedUtc" TEXT NOT NULL,
				"AcceptedUtc" TEXT NULL,
				"RevokedUtc" TEXT NULL,
				CONSTRAINT "FK_PlatformUserInvitations_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
			);
			""",
			cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection,
			"""
			CREATE TABLE IF NOT EXISTS "PlatformThemes" (
				"Id" INTEGER NOT NULL CONSTRAINT "PK_PlatformThemes" PRIMARY KEY AUTOINCREMENT,
				"Key" TEXT NOT NULL,
				"Name" TEXT NOT NULL,
				"Description" TEXT NOT NULL,
				"PrimaryColor" TEXT NOT NULL,
				"AccentColor" TEXT NOT NULL,
				"BackgroundColor" TEXT NOT NULL,
				"CssVariablesBlock" TEXT NOT NULL,
				"IsEnabled" INTEGER NOT NULL DEFAULT 1,
				"IsArchived" INTEGER NOT NULL DEFAULT 0,
				"CreatedUtc" TEXT NOT NULL,
				"UpdatedUtc" TEXT NOT NULL
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

		await ExecuteSqliteNonQueryAsync(connection, """DROP INDEX IF EXISTS "IX_Tenants_Slug";""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_Tenants_Slug" ON "Tenants" ("Slug");""", cancellationToken);
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
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_PlatformUserInvitations_TokenHash" ON "PlatformUserInvitations" ("TokenHash");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_PlatformUserInvitations_Email" ON "PlatformUserInvitations" ("Email");""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_PlatformThemes_Key" ON "PlatformThemes" ("Key");""", cancellationToken);
		await EnsureSqliteColumnExistsAsync(connection, "PlatformThemes", "IsEnabled", """ALTER TABLE "PlatformThemes" ADD COLUMN "IsEnabled" INTEGER NOT NULL DEFAULT 1;""", cancellationToken);
		await EnsureSqliteColumnExistsAsync(connection, "PlatformThemes", "IsArchived", """ALTER TABLE "PlatformThemes" ADD COLUMN "IsArchived" INTEGER NOT NULL DEFAULT 0;""", cancellationToken);
		await EnsureSqliteColumnExistsAsync(connection, "SiteSettings", "InitialSetupCompletedUtc", """ALTER TABLE "SiteSettings" ADD COLUMN "InitialSetupCompletedUtc" TEXT NULL;""", cancellationToken);
		await ExecuteSqliteNonQueryAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TenantId_CreatedUtc" ON "AuditLogs" ("TenantId", "CreatedUtc");""", cancellationToken);
		if (await SqliteTableExistsAsync(connection, "PostalAddresses", cancellationToken))
		{
			await ExecuteSqliteNonQueryAsync(connection, """DROP INDEX IF EXISTS "IX_PostalAddresses_NormalizedKey";""", cancellationToken);
			await ExecuteSqliteNonQueryAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_PostalAddresses_TenantId_NormalizedKey" ON "PostalAddresses" ("TenantId", "NormalizedKey");""", cancellationToken);
		}
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

	private static async Task RepairLegacySqlServerSchemaAsync(string connectionString, CancellationToken cancellationToken)
	{
		await using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync(cancellationToken);

		await EnsureSqlServerColumnExistsAsync(
			connection,
			"dbo",
			"SiteSettings",
			"InitialSetupCompletedUtc",
			"ALTER TABLE [dbo].[SiteSettings] ADD [InitialSetupCompletedUtc] datetimeoffset NULL;",
			cancellationToken);
	}

	private static async Task EnsureSqlServerColumnExistsAsync(
		SqlConnection connection,
		string schemaName,
		string tableName,
		string columnName,
		string alterSql,
		CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT COUNT(*)
			FROM INFORMATION_SCHEMA.COLUMNS
			WHERE TABLE_SCHEMA = @schemaName
				AND TABLE_NAME = @tableName
				AND COLUMN_NAME = @columnName;
			""";
		command.Parameters.AddWithValue("@schemaName", schemaName);
		command.Parameters.AddWithValue("@tableName", tableName);
		command.Parameters.AddWithValue("@columnName", columnName);

		var exists = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
		if (exists)
		{
			return;
		}

		await using var alterCommand = connection.CreateCommand();
		alterCommand.CommandText = alterSql;
		await alterCommand.ExecuteNonQueryAsync(cancellationToken);
	}
}
