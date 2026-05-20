using Survey.Application.Models;

namespace Survey.Application.Services;

public interface IQueuedEmailService
{
	Task<QueuedEmailResult> QueueEmailAsync(QueuedEmailMessage message, CancellationToken cancellationToken = default);
}
