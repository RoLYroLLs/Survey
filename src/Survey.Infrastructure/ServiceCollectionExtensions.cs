using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Survey.Application.Services;
using Survey.Infrastructure.Configuration;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Persistence;
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
				options.SignIn.RequireConfirmedAccount = false;
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

		services.AddScoped<SurveyApplicationService>();
		services.AddScoped<ISurveyAdministrationService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<ISurveyExperienceService>(serviceProvider => serviceProvider.GetRequiredService<SurveyApplicationService>());
		services.AddScoped<IdentityDataSeeder>();
		services.AddScoped<GeographyDataSeeder>();
		services.AddScoped<SiteSettingsSeeder>();

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

		var geographySeeder = serviceProvider.GetRequiredService<GeographyDataSeeder>();
		var forceGeographySeed = configuration.GetValue<bool>("Seeding:ForceGeography");
		await geographySeeder.SeedAsync(forceGeographySeed, cancellationToken);

		var siteSettingsSeeder = serviceProvider.GetRequiredService<SiteSettingsSeeder>();
		await siteSettingsSeeder.SeedAsync(cancellationToken);

		var seeder = serviceProvider.GetRequiredService<IdentityDataSeeder>();
		await seeder.SeedAsync(cancellationToken);
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
}
