using System.ComponentModel.DataAnnotations;

namespace Survey.Application.Models;

public sealed class PlatformThemeListItem
{
	public int Id { get; set; }
	public string Key { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string PrimaryColor { get; set; } = string.Empty;
	public string AccentColor { get; set; } = string.Empty;
	public string BackgroundColor { get; set; } = string.Empty;
	public bool IsEnabled { get; set; }
	public bool IsArchived { get; set; }
	public bool IsDefaultTheme { get; set; }
	public int TenantUsageCount { get; set; }
	public DateTimeOffset UpdatedUtc { get; set; }
}

public sealed class PlatformThemeEditModel
{
	public int? Id { get; set; }

	[Required]
	[StringLength(100)]
	public string Key { get; set; } = string.Empty;

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(1000)]
	public string Description { get; set; } = string.Empty;

	[Required]
	[StringLength(32)]
	public string PrimaryColor { get; set; } = "#0d8f81";

	[Required]
	[StringLength(32)]
	public string AccentColor { get; set; } = "#15394a";

	[Required]
	[StringLength(32)]
	public string BackgroundColor { get; set; } = "#eef4f2";

	[Required]
	[StringLength(12000)]
	public string CssVariablesBlock { get; set; } = string.Empty;

	public bool IsEnabled { get; set; } = true;
	public bool IsArchived { get; set; }
	public bool IsDefaultTheme { get; set; }
	public int TenantUsageCount { get; set; }
	public int? ReplacementThemeId { get; set; }
	public IReadOnlyList<SelectOption> ReplacementThemeOptions { get; set; } = Array.Empty<SelectOption>();
	public DateTimeOffset? UpdatedUtc { get; set; }
	public bool IsNew => !Id.HasValue;
	public bool RequiresReplacement => IsDefaultTheme || TenantUsageCount > 0;
}

public sealed class ThemeSeedModel
{
	public string Key { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string PrimaryColor { get; set; } = string.Empty;
	public string AccentColor { get; set; } = string.Empty;
	public string BackgroundColor { get; set; } = string.Empty;
	public string CssVariablesBlock { get; set; } = string.Empty;
}
