namespace Survey.Infrastructure.Configuration;

public sealed class AppOptions
{
	public const string SectionName = "App";

	public string? PublicOrigin { get; set; }
}
