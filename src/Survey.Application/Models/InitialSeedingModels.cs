namespace Survey.Application.Models;

public static class InitialSeedingStages
{
	public const string Roles = "roles";
	public const string PlatformThemes = "platform-themes";
	public const string SiteSettings = "site-settings";
	public const string Countries = "countries";
	public const string States = "states";
	public const string Counties = "counties";
	public const string ZipMappings = "zip-mappings";

	public static readonly IReadOnlyList<InitialSeedingStageDefinition> IdentityOrdered =
	[
		new(Roles, "Roles")
	];

	public static readonly IReadOnlyList<InitialSeedingStageDefinition> GeographyOrdered =
	[
		new(Countries, "Countries"),
		new(States, "States/Territories"),
		new(Counties, "Counties/FIPS"),
		new(ZipMappings, "ZIP/FIPS Mappings")
	];

	public static readonly IReadOnlyList<InitialSeedingStageDefinition> FinalizationOrdered =
	[
		new(PlatformThemes, "Platform Themes"),
		new(SiteSettings, "Site Settings")
	];

	public static readonly IReadOnlyList<InitialSeedingStageDefinition> Ordered = IdentityOrdered
		.Concat(GeographyOrdered)
		.Concat(FinalizationOrdered)
		.ToArray();

	public static string GetLabel(string stageKey)
	{
		return Ordered.FirstOrDefault(stage => string.Equals(stage.Key, stageKey, StringComparison.Ordinal))?.Label
			?? stageKey;
	}
}

public sealed record InitialSeedingStageDefinition(string Key, string Label);

public sealed class InitialSeedingProgressUpdate
{
	public string StageKey { get; init; } = string.Empty;

	public string StageLabel { get; init; } = string.Empty;

	public string ActivityMessage { get; init; } = string.Empty;

	public int Processed { get; init; }

	public int Total { get; init; }

	public bool IsComplete { get; init; }
}

public sealed class InitialSeedingProgressSnapshot
{
	public bool IsRunning { get; init; }

	public bool IsComplete { get; init; }

	public string? ErrorMessage { get; init; }

	public IReadOnlyList<InitialSeedingStageSnapshot> Stages { get; init; } = Array.Empty<InitialSeedingStageSnapshot>();
}

public sealed class InitialSeedingStageSnapshot
{
	public string StageKey { get; init; } = string.Empty;

	public string StageLabel { get; init; } = string.Empty;

	public string ActivityMessage { get; init; } = string.Empty;

	public int Processed { get; init; }

	public int Total { get; init; }

	public bool IsStarted { get; init; }

	public bool IsComplete { get; init; }
}

public sealed class InitialSeedingActivityEntry
{
	public long Sequence { get; init; }

	public string StageKey { get; init; } = string.Empty;

	public string StageLabel { get; init; } = string.Empty;

	public string Message { get; init; } = string.Empty;
}

public sealed class InitialSeedingPlaybackEntry
{
	public long Sequence { get; init; }

	public string StageKey { get; init; } = string.Empty;

	public string StageLabel { get; init; } = string.Empty;

	public string ActivityMessage { get; init; } = string.Empty;

	public int Processed { get; init; }

	public int Total { get; init; }

	public bool IsComplete { get; init; }
}
