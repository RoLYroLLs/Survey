using System.Security.Cryptography;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Configuration;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

public sealed class QueuedEmailService(
	SurveyDbContext dbContext,
	BackgroundOperationsService backgroundOperationsService,
	IBackgroundJobClient backgroundJobClient,
	IOptions<BackgroundJobsOptions> options) : IQueuedEmailService
{
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly BackgroundOperationsService _backgroundOperationsService = backgroundOperationsService;
	private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
	private readonly BackgroundJobsOptions _options = options.Value;

	public async Task<QueuedEmailResult> QueueEmailAsync(QueuedEmailMessage message, CancellationToken cancellationToken = default)
	{
		var trackingToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
		var trackedHtmlBody = EmailTrackingMarkupBuilder.AttachTracking(
			message.HtmlBody,
			message.BaseUrl,
			trackingToken);
		var summary = $"Send '{message.Subject}' to {message.RecipientEmail}.";
		var operationId = await _backgroundOperationsService.CreateOperationAsync(
			BackgroundOperationKinds.OutboundEmailDispatch,
			_options.EmailQueueName,
			summary,
			message.TenantId,
			message.RequestedByUserId,
			metadataJson: string.Empty,
			stageStatesJson: string.Empty,
			cancellationToken);

		var email = new OutboundEmail(
			message.TemplateKey,
			message.SourceType,
			message.SourceId,
			message.RecipientEmail,
			message.Subject,
			trackedHtmlBody,
			message.TextBody,
			trackingToken,
			message.TenantId,
			message.RequestedByUserId,
			message.RecipientName);
		email.AttachOperation(operationId);
		_dbContext.OutboundEmails.Add(email);
		await _dbContext.SaveChangesAsync(cancellationToken);

		var jobId = _backgroundJobClient.Create(
			Job.FromExpression<EmailHangfireJobRunner>(runner => runner.SendOutboundEmailAsync(email.Id, CancellationToken.None)),
			new EnqueuedState(_options.EmailQueueName));
		await _backgroundOperationsService.AttachHangfireJobIdAsync(operationId, jobId, cancellationToken);

		return new QueuedEmailResult
		{
			OutboundEmailId = email.Id,
			BackgroundOperationId = operationId,
			TrackingToken = trackingToken
		};
	}
}
