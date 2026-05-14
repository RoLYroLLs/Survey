namespace Survey.Domain;

public class SiteSetting
{
	public const int DefaultId = 1;

	public int Id { get; private set; } = DefaultId;
	public string ThemePresetKey { get; private set; } = string.Empty;
	public DateTimeOffset UpdatedUtc { get; private set; }

	private SiteSetting()
	{
	}

	public SiteSetting(string themePresetKey)
	{
		UpdateThemePreset(themePresetKey);
	}

	public void UpdateThemePreset(string themePresetKey)
	{
		ThemePresetKey = RequireValue(themePresetKey, nameof(themePresetKey), 100);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	private static string RequireValue(string? value, string paramName, int maxLength)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			throw new ArgumentException("A value is required.", paramName);
		}

		return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
	}
}
