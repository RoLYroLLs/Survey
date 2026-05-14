using System.Text.Json;

namespace Survey.Web.Tests;

public class WebConfigurationTests
{
	[Fact]
	public void AppSettings_Expose_Database_Provider_And_Seed_Admin_Sections()
	{
		var filePath = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..",
			"..",
			"..",
			"..",
			"..",
			"src",
			"Survey.Web",
			"appsettings.json"));

		using var document = JsonDocument.Parse(File.ReadAllText(filePath));
		var root = document.RootElement;

		Assert.True(root.TryGetProperty("Database", out var databaseSection));
		Assert.Equal("Sqlite", databaseSection.GetProperty("Provider").GetString());
		Assert.True(root.TryGetProperty("SeedAdmin", out var seedAdminSection));
		Assert.True(seedAdminSection.TryGetProperty("Email", out _));
	}
}
