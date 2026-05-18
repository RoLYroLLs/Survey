using System.Text.Json;

namespace Survey.Web.Tests;

public class WebConfigurationTests
{
	[Fact]
	public void AppSettings_Expose_Sqlite_Database_Settings_Without_Seed_Admin()
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
		Assert.False(root.TryGetProperty("SeedAdmin", out _));
	}
}
