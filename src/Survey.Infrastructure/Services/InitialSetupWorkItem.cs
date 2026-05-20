namespace Survey.Infrastructure.Services;

public sealed class InitialSetupWorkItem
{
	public int OperationId { get; init; }

	public string[] SelectedThemeKeys { get; init; } = [];

	public string DefaultThemeKey { get; init; } = string.Empty;

	public bool ResetBeforeRun { get; init; }
}
