using Survey.Application.Models;

namespace Survey.Application.Services;

public interface IInitialSetupJobService
{
	Task<InitialSetupJobStartResult> StartOrResumeAsync(IReadOnlyCollection<string> selectedThemeKeys, string defaultThemeKey, string? requestedByUserId, CancellationToken cancellationToken = default);
	Task<InitialSetupJobStartResult> RetryAsync(IReadOnlyCollection<string> selectedThemeKeys, string defaultThemeKey, string? requestedByUserId, CancellationToken cancellationToken = default);
}
