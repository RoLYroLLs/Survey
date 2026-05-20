using Microsoft.Extensions.Logging;
using Survey.Application.Models;
using Survey.Application.Services;

namespace Survey.Infrastructure.Services;

public sealed class NoOpEmailTransport(ILogger<NoOpEmailTransport> logger) : IEmailTransport
{
	private readonly ILogger<NoOpEmailTransport> _logger = logger;

	public Task<EmailTransportResult> SendAsync(EmailTransportMessage message, CancellationToken cancellationToken = default)
	{
		var providerMessageId = $"noop:{Guid.NewGuid():N}";
		_logger.LogInformation("No-op email transport captured message to {RecipientEmail} with subject {Subject}. Provider message id {ProviderMessageId}.", message.RecipientEmail, message.Subject, providerMessageId);

		return Task.FromResult(new EmailTransportResult
		{
			Succeeded = true,
			ProviderMessageId = providerMessageId
		});
	}
}
