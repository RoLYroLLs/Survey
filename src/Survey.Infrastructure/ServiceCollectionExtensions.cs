using Microsoft.AspNetCore.Identity;
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
		}

		var dbContext = serviceProvider.GetRequiredService<SurveyDbContext>();
		await dbContext.Database.MigrateAsync(cancellationToken);

		var geographySeeder = serviceProvider.GetRequiredService<GeographyDataSeeder>();
		await geographySeeder.SeedAsync(cancellationToken);

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
}
