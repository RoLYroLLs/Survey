using Survey.Application.Models;

namespace Survey.Application.Services;

public interface IEmailTransport
{
	Task<EmailTransportResult> SendAsync(EmailTransportMessage message, CancellationToken cancellationToken = default);
}
