using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Survey.Application.Models;

public class SiteAppearanceModel
{
	public string ThemePresetKey { get; set; } = SiteThemePresetCatalog.DefaultPresetKey;
	public string ThemePresetName { get; set; } = string.Empty;
	public string CssVariablesBlock { get; set; } = string.Empty;
}

public class SiteSettingsEditModel
{
	[Required]
	public string ThemePresetKey { get; set; } = SiteThemePresetCatalog.DefaultPresetKey;

	public DateTimeOffset? UpdatedUtc { get; set; }
	public IReadOnlyList<ThemePresetOption> PresetOptions { get; set; } = Array.Empty<ThemePresetOption>();
}

public class ThemePresetOption
{
	public string Key { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string PrimaryColor { get; set; } = string.Empty;
	public string AccentColor { get; set; } = string.Empty;
	public string BackgroundColor { get; set; } = string.Empty;
	public string PreviewStyle { get; set; } = string.Empty;
}

public static class SiteThemePresetCatalog
{
	public const string DefaultPresetKey = "coastal-current";

	private static readonly IReadOnlyList<SiteThemePresetDefinition> Definitions =
	[
		new(
			"coastal-current",
			"Coastal Current",
			"The existing calm teal system with soft sea-glass surfaces.",
			"#eef4f2",
			"#11212a",
			"#5f727a",
			"rgba(17, 33, 42, 0.08)",
			"rgba(255, 255, 255, 0.9)",
			"#0d8f81",
			"#0a6f64",
			"#15394a",
			"#f8fbfa",
			"#c4544e",
			"0 18px 38px rgba(9, 27, 35, 0.08)",
			"rgba(13, 143, 129, 0.12)",
			"rgba(13, 143, 129, 0.1)",
			"rgba(13, 143, 129, 0.16)",
			"rgba(17, 33, 42, 0.08)",
			"rgba(196, 84, 78, 0.08)",
			"rgba(196, 84, 78, 0.2)",
			"rgba(9, 27, 35, 0.96)",
			"rgba(22, 45, 56, 0.08)",
			"#e7f3f0",
			"rgba(231, 243, 240, 0.76)",
			"rgba(255, 255, 255, 0.08)",
			"rgba(231, 243, 240, 0.86)",
			"#ffffff",
			"rgba(9, 168, 150, 0.18)",
			"#f7fbfa",
			"#eef4f2",
			"13, 143, 129"),
		new(
			"sunrise-coral",
			"Sunrise Coral",
			"Warm coral and sandstone tones for a brighter front-door experience.",
			"#faf1eb",
			"#2f1e22",
			"#806060",
			"rgba(47, 30, 34, 0.1)",
			"rgba(255, 250, 247, 0.92)",
			"#d96d5b",
			"#b65446",
			"#7b3e37",
			"#fff8f4",
			"#b4434d",
			"0 20px 40px rgba(83, 38, 36, 0.12)",
			"rgba(217, 109, 91, 0.14)",
			"rgba(217, 109, 91, 0.1)",
			"rgba(217, 109, 91, 0.18)",
			"rgba(47, 30, 34, 0.08)",
			"rgba(180, 67, 77, 0.08)",
			"rgba(180, 67, 77, 0.2)",
			"rgba(56, 28, 32, 0.97)",
			"rgba(123, 62, 55, 0.16)",
			"#fff4ee",
			"rgba(255, 227, 218, 0.8)",
			"rgba(255, 255, 255, 0.08)",
			"rgba(255, 240, 233, 0.9)",
			"#ffffff",
			"rgba(232, 126, 93, 0.2)",
			"#fff7f2",
			"#faf1eb",
			"217, 109, 91"),
		new(
			"harbor-blue",
			"Harbor Blue",
			"Deep blue navigation with clear steel-blue actions and cool panels.",
			"#edf3fb",
			"#132235",
			"#60738b",
			"rgba(19, 34, 53, 0.1)",
			"rgba(255, 255, 255, 0.92)",
			"#2d6cb3",
			"#214f86",
			"#16395f",
			"#f7fbff",
			"#c24b4b",
			"0 18px 40px rgba(19, 34, 53, 0.12)",
			"rgba(45, 108, 179, 0.14)",
			"rgba(45, 108, 179, 0.1)",
			"rgba(45, 108, 179, 0.18)",
			"rgba(19, 34, 53, 0.08)",
			"rgba(194, 75, 75, 0.08)",
			"rgba(194, 75, 75, 0.18)",
			"rgba(15, 27, 43, 0.97)",
			"rgba(33, 79, 134, 0.16)",
			"#ebf4ff",
			"rgba(215, 229, 247, 0.78)",
			"rgba(255, 255, 255, 0.08)",
			"rgba(230, 240, 252, 0.9)",
			"#ffffff",
			"rgba(70, 139, 214, 0.18)",
			"#f7fbff",
			"#edf3fb",
			"45, 108, 179"),
		new(
			"citrus-grove",
			"Citrus Grove",
			"Fresh green and orange accents that feel rooted in community outreach.",
			"#f3f7ee",
			"#19281c",
			"#62705f",
			"rgba(25, 40, 28, 0.09)",
			"rgba(255, 255, 255, 0.9)",
			"#5f9b3d",
			"#44712b",
			"#c96f2d",
			"#fbfdf8",
			"#b84d43",
			"0 18px 38px rgba(39, 56, 31, 0.1)",
			"rgba(95, 155, 61, 0.14)",
			"rgba(95, 155, 61, 0.1)",
			"rgba(95, 155, 61, 0.18)",
			"rgba(25, 40, 28, 0.08)",
			"rgba(184, 77, 67, 0.08)",
			"rgba(184, 77, 67, 0.2)",
			"rgba(24, 40, 21, 0.97)",
			"rgba(68, 113, 43, 0.18)",
			"#eff6e8",
			"rgba(225, 236, 214, 0.78)",
			"rgba(255, 255, 255, 0.08)",
			"rgba(233, 244, 223, 0.9)",
			"#ffffff",
			"rgba(124, 190, 72, 0.18)",
			"#f8fbee",
			"#f3f7ee",
			"95, 155, 61"),
		new(
			"golden-hour",
			"Golden Hour",
			"Amber highlights with slate text for a warm, modern civic look.",
			"#fbf5e8",
			"#2f2616",
			"#7e6e4f",
			"rgba(47, 38, 22, 0.1)",
			"rgba(255, 252, 245, 0.9)",
			"#c58a1b",
			"#9a6810",
			"#6f4e20",
			"#fffaf1",
			"#b84f47",
			"0 18px 40px rgba(68, 49, 20, 0.12)",
			"rgba(197, 138, 27, 0.14)",
			"rgba(197, 138, 27, 0.1)",
			"rgba(197, 138, 27, 0.18)",
			"rgba(47, 38, 22, 0.08)",
			"rgba(184, 79, 71, 0.08)",
			"rgba(184, 79, 71, 0.2)",
			"rgba(41, 31, 18, 0.97)",
			"rgba(111, 78, 32, 0.18)",
			"#fff3da",
			"rgba(245, 226, 183, 0.78)",
			"rgba(255, 255, 255, 0.08)",
			"rgba(255, 243, 214, 0.9)",
			"#ffffff",
			"rgba(222, 173, 59, 0.2)",
			"#fff9ee",
			"#fbf5e8",
			"197, 138, 27"),
		new(
			"berry-slate",
			"Berry Slate",
			"Muted berry actions paired with neutral slate surfaces and dark chrome.",
			"#f4eff4",
			"#231b29",
			"#6f6279",
			"rgba(35, 27, 41, 0.1)",
			"rgba(255, 255, 255, 0.9)",
			"#9a577f",
			"#76415f",
			"#4b344f",
			"#fbf8fb",
			"#bf4e53",
			"0 18px 40px rgba(35, 27, 41, 0.12)",
			"rgba(154, 87, 127, 0.14)",
			"rgba(154, 87, 127, 0.1)",
			"rgba(154, 87, 127, 0.18)",
			"rgba(35, 27, 41, 0.08)",
			"rgba(191, 78, 83, 0.08)",
			"rgba(191, 78, 83, 0.2)",
			"rgba(31, 24, 37, 0.97)",
			"rgba(75, 52, 79, 0.18)",
			"#f3ebf2",
			"rgba(227, 214, 229, 0.78)",
			"rgba(255, 255, 255, 0.08)",
			"rgba(241, 232, 243, 0.9)",
			"#ffffff",
			"rgba(160, 101, 138, 0.2)",
			"#faf6fa",
			"#f4eff4",
			"154, 87, 127"),
		new(
			"monochrome-edge",
			"Monochrome Edge",
			"A crisp black-and-white theme with restrained contrast and a subtle graphite accent.",
			"#f4f4f4",
			"#141414",
			"#6d6d6d",
			"rgba(20, 20, 20, 0.1)",
			"rgba(255, 255, 255, 0.96)",
			"#111111",
			"#000000",
			"#2a2a2a",
			"#fafafa",
			"#8c2f2f",
			"0 18px 36px rgba(0, 0, 0, 0.08)",
			"rgba(17, 17, 17, 0.12)",
			"rgba(17, 17, 17, 0.06)",
			"rgba(17, 17, 17, 0.18)",
			"rgba(20, 20, 20, 0.06)",
			"rgba(140, 47, 47, 0.08)",
			"rgba(140, 47, 47, 0.18)",
			"#050505",
			"rgba(255, 255, 255, 0.08)",
			"#f8f8f8",
			"rgba(255, 255, 255, 0.56)",
			"rgba(255, 255, 255, 0.08)",
			"rgba(255, 255, 255, 0.84)",
			"#ffffff",
			"rgba(0, 0, 0, 0.04)",
			"#f7f7f7",
			"#efefef",
			"17, 17, 17"),
	];

	public static IReadOnlyList<ThemePresetOption> GetOptions()
	{
		return Definitions
			.Select(definition => new ThemePresetOption
			{
				Key = definition.Key,
				Name = definition.Name,
				Description = definition.Description,
				PrimaryColor = definition.Primary,
				AccentColor = definition.Accent,
				BackgroundColor = definition.Background,
				PreviewStyle = $"background: linear-gradient(135deg, {definition.Primary}, {definition.Accent});"
			})
			.ToArray();
	}

	public static string GetPresetName(string? key)
	{
		return GetDefinition(key).Name;
	}

	public static string BuildCssVariablesBlock(string? key)
	{
		var definition = GetDefinition(key);
		var builder = new StringBuilder();
		builder.AppendLine(":root {");
		builder.AppendLine($"\t--app-bg: {definition.Background};");
		builder.AppendLine($"\t--app-ink: {definition.Ink};");
		builder.AppendLine($"\t--app-muted: {definition.Muted};");
		builder.AppendLine($"\t--app-border: {definition.Border};");
		builder.AppendLine($"\t--app-card: {definition.Card};");
		builder.AppendLine($"\t--app-primary: {definition.Primary};");
		builder.AppendLine($"\t--app-primary-strong: {definition.PrimaryStrong};");
		builder.AppendLine($"\t--app-accent: {definition.Accent};");
		builder.AppendLine($"\t--app-warm: {definition.Warm};");
		builder.AppendLine($"\t--app-danger: {definition.Danger};");
		builder.AppendLine($"\t--app-shadow: {definition.Shadow};");
		builder.AppendLine($"\t--app-primary-soft: {definition.PrimarySoft};");
		builder.AppendLine($"\t--app-primary-tint: {definition.PrimaryTint};");
		builder.AppendLine($"\t--app-primary-border: {definition.PrimaryBorder};");
		builder.AppendLine($"\t--app-muted-surface: {definition.MutedSurface};");
		builder.AppendLine($"\t--app-danger-soft: {definition.DangerSoft};");
		builder.AppendLine($"\t--app-danger-border: {definition.DangerBorder};");
		builder.AppendLine($"\t--app-sidebar-bg: {definition.SidebarBackground};");
		builder.AppendLine($"\t--app-sidebar-border: {definition.SidebarBorder};");
		builder.AppendLine($"\t--app-sidebar-ink: {definition.SidebarInk};");
		builder.AppendLine($"\t--app-sidebar-muted: {definition.SidebarMuted};");
		builder.AppendLine($"\t--app-sidebar-surface: {definition.SidebarSurface};");
		builder.AppendLine($"\t--app-sidebar-link: {definition.SidebarLink};");
		builder.AppendLine($"\t--app-sidebar-link-hover: {definition.SidebarLinkHover};");
		builder.AppendLine($"\t--app-shell-glow: {definition.ShellGlow};");
		builder.AppendLine($"\t--app-shell-start: {definition.ShellStart};");
		builder.AppendLine($"\t--app-shell-end: {definition.ShellEnd};");
		builder.AppendLine($"\t--bs-primary: {definition.Primary};");
		builder.AppendLine($"\t--bs-primary-rgb: {definition.PrimaryRgb};");
		builder.AppendLine($"\t--bs-link-color: {definition.Primary};");
		builder.AppendLine($"\t--bs-link-hover-color: {definition.PrimaryStrong};");
		builder.AppendLine($"\t--bs-focus-ring-color: rgba({definition.PrimaryRgb}, 0.25);");
		builder.AppendLine("}");
		return builder.ToString();
	}

	public static bool IsValidPresetKey(string? key)
	{
		return Definitions.Any(definition => string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase));
	}

	private static SiteThemePresetDefinition GetDefinition(string? key)
	{
		return Definitions.FirstOrDefault(definition => string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase))
			?? Definitions.First(definition => definition.Key == DefaultPresetKey);
	}

	private sealed record SiteThemePresetDefinition(
		string Key,
		string Name,
		string Description,
		string Background,
		string Ink,
		string Muted,
		string Border,
		string Card,
		string Primary,
		string PrimaryStrong,
		string Accent,
		string Warm,
		string Danger,
		string Shadow,
		string PrimarySoft,
		string PrimaryTint,
		string PrimaryBorder,
		string MutedSurface,
		string DangerSoft,
		string DangerBorder,
		string SidebarBackground,
		string SidebarBorder,
		string SidebarInk,
		string SidebarMuted,
		string SidebarSurface,
		string SidebarLink,
		string SidebarLinkHover,
		string ShellGlow,
		string ShellStart,
		string ShellEnd,
		string PrimaryRgb);
}
