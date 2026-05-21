using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

public sealed class EmailHangfireJobRunner(
	SurveyDbContext dbContext,
	IServiceScopeFactory serviceScopeFactory,
	IEmailTransport emailTransport)
{
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
	private readonly IEmailTransport _emailTransport = emailTransport;

	public async Task SendOutboundEmailAsync(int outboundEmailId, CancellationToken cancellationToken)
	{
		var email = await _dbContext.OutboundEmails
			.Include(item => item.Operation)
			.FirstOrDefaultAsync(item => item.Id == outboundEmailId, cancellationToken)
			?? throw new InvalidOperationException("The outbound email was not found.");
		if (!email.BackgroundOperationId.HasValue)
		{
			throw new InvalidOperationException("The outbound email is missing its background operation.");
		}

		await UseBackgroundOperationsServiceAsync(
			service => service.MarkRunningAsync(email.BackgroundOperationId.Value, $"Sending email to '{email.RecipientEmail}'.", cancellationToken));
		email.MarkSending();
		email.IncrementAttempt();
		var attempt = new OutboundEmailAttempt(email.Id, email.AttemptCount);
		_dbContext.OutboundEmailAttempts.Add(attempt);
		await _dbContext.SaveChangesAsync(cancellationToken);

		var result = await _emailTransport.SendAsync(new EmailTransportMessage
		{
			RecipientEmail = email.RecipientEmail,
			RecipientName = email.RecipientName,
			Subject = email.Subject,
			HtmlBody = email.HtmlBody,
			TextBody = email.TextBody
		}, cancellationToken);

		if (result.Succeeded)
		{
			email.MarkSent(result.ProviderMessageId);
			attempt.MarkSent(result.ProviderMessageId);
			await _dbContext.SaveChangesAsync(cancellationToken);
			await UseBackgroundOperationsServiceAsync(
				service => service.CompleteOperationAsync(email.BackgroundOperationId.Value, $"Sent email to '{email.RecipientEmail}'.", cancellationToken));
			return;
		}

		email.MarkFailed(result.ErrorMessage, result.ProviderMessageId);
		attempt.MarkFailed(result.ErrorMessage, result.ProviderMessageId);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await UseBackgroundOperationsServiceAsync(
			service => service.FailOperationAsync(email.BackgroundOperationId.Value, result.ErrorMessage, cancellationToken));
		throw new InvalidOperationException(result.ErrorMessage);
	}

	private async Task UseBackgroundOperationsServiceAsync(Func<BackgroundOperationsService, Task> action)
	{
		using var scope = _serviceScopeFactory.CreateScope();
		var service = scope.ServiceProvider.GetRequiredService<BackgroundOperationsService>();
		await action(service);
	}
}
