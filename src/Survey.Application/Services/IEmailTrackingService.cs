using Survey.Application.Models;

namespace Survey.Application.Services;

public interface IEmailTrackingService
{
	Task TrackOpenAsync(string token, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default);
	Task<EmailClickRedirectResult> TrackClickAsync(string token, string? linkType, string? destinationUrl, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default);
}
