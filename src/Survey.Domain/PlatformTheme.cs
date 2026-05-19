namespace Survey.Domain;

public class PlatformTheme
{
	public int Id { get; private set; }
	public string Key { get; private set; } = string.Empty;
	public string Name { get; private set; } = string.Empty;
	public string Description { get; private set; } = string.Empty;
	public string PrimaryColor { get; private set; } = string.Empty;
	public string AccentColor { get; private set; } = string.Empty;
	public string BackgroundColor { get; private set; } = string.Empty;
	public string CssVariablesBlock { get; private set; } = string.Empty;
	public bool IsEnabled { get; private set; }
	public bool IsArchived { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }

	private PlatformTheme()
	{
	}

	public PlatformTheme(
		string key,
		string name,
		string description,
		string primaryColor,
		string accentColor,
		string backgroundColor,
		string cssVariablesBlock)
	{
		UpdateIdentity(key, name, description);
		UpdatePresentation(primaryColor, accentColor, backgroundColor, cssVariablesBlock);
		IsEnabled = true;
		IsArchived = false;
		CreatedUtc = UpdatedUtc;
	}

	public void UpdateIdentity(string key, string name, string description)
	{
		Key = RequireValue(key, nameof(key), 100);
		Name = RequireValue(name, nameof(name), 200);
		Description = RequireValue(description, nameof(description), 1000);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void UpdatePresentation(string primaryColor, string accentColor, string backgroundColor, string cssVariablesBlock)
	{
		PrimaryColor = RequireValue(primaryColor, nameof(primaryColor), 32);
		AccentColor = RequireValue(accentColor, nameof(accentColor), 32);
		BackgroundColor = RequireValue(backgroundColor, nameof(backgroundColor), 32);
		CssVariablesBlock = RequireValue(cssVariablesBlock, nameof(cssVariablesBlock), 12000);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void Enable()
	{
		IsEnabled = true;
		IsArchived = false;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void Disable()
	{
		IsEnabled = false;
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void Archive()
	{
		IsArchived = true;
		IsEnabled = false;
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
