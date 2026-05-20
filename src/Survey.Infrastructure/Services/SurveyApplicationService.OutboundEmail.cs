using System.Net;
using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<QueuedEmailResult> SendAssignmentEmailAsync(int assignmentId, string baseUrl, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.AssignmentsSend, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		var requestedByUserId = await RequireCurrentUserIdAsync(cancellationToken);
		var assignment = await _dbContext.SurveyAssignments
			.AsNoTracking()
			.Include(item => item.Location)
				.ThenInclude(location => location.Person)
			.Include(item => item.LocationEmail)
			.Include(item => item.SurveyVersion)
				.ThenInclude(version => version.SurveyDefinition)
			.Include(item => item.Response)
			.FirstOrDefaultAsync(item => item.Id == assignmentId && item.TenantId == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested assignment was not found.");

		if (assignment.IsArchived)
		{
			throw new InvalidOperationException("Archived assignments cannot be emailed.");
		}

		if (assignment.Response is not null)
		{
			throw new InvalidOperationException("Completed assignments cannot be emailed.");
		}

		if (assignment.IsExpired(DateTimeOffset.UtcNow))
		{
			throw new InvalidOperationException("Expired assignments cannot be emailed.");
		}

		var locationEmail = assignment.LocationEmail
			?? throw new InvalidOperationException("Select a location email before sending this assignment.");
		var recipientName = BuildFullName(
			assignment.Location.Person.FirstName,
			assignment.Location.Person.MiddleName,
			assignment.Location.Person.LastName);
		if (string.IsNullOrWhiteSpace(recipientName))
		{
			recipientName = locationEmail.EmailAddress;
		}

		var surveyName = assignment.SurveyVersion.SurveyDefinition.Name;
		var versionName = assignment.SurveyVersion.DisplayName;
		var subject = $"{surveyName} survey invitation";
		var surveyUrl = BuildAbsoluteUrl(baseUrl, $"/survey/{Uri.EscapeDataString(assignment.PublicToken)}");
		var expiresLine = assignment.ExpiresAtUtc.HasValue
			? $"This link expires {FormatUtc(assignment.ExpiresAtUtc.Value)}."
			: "This survey link does not expire.";
		var htmlBody =
			$"<p>Hello {HtmlEncode(recipientName)},</p>" +
			$"<p>You have a new survey to complete for <strong>{HtmlEncode(surveyName)}</strong> ({HtmlEncode(versionName)}).</p>" +
			$"<p><a href=\"{HtmlEncode(surveyUrl)}\">Open your survey</a></p>" +
			$"<p>{HtmlEncode(expiresLine)}</p>";
		var textBody =
			$"Hello {recipientName},{Environment.NewLine}{Environment.NewLine}" +
			$"You have a new survey to complete for {surveyName} ({versionName}).{Environment.NewLine}" +
			$"Open your survey: {surveyUrl}{Environment.NewLine}" +
			$"{expiresLine}";
		var result = await _queuedEmailService.QueueEmailAsync(
			new QueuedEmailMessage
			{
				TemplateKey = "survey-assignment",
				SourceType = OutboundEmailSourceTypes.Assignment,
				SourceId = assignment.Id.ToString(),
				RecipientName = recipientName,
				RecipientEmail = locationEmail.EmailAddress,
				Subject = subject,
				HtmlBody = htmlBody,
				TextBody = textBody,
				BaseUrl = NormalizeBaseUrl(baseUrl),
				TenantId = assignment.TenantId,
				RequestedByUserId = requestedByUserId
			},
			cancellationToken);
		await AuditTenantEntityChangeAsync(
			"tenant.assignment.email-queued",
			nameof(SurveyAssignment),
			assignment.Id,
			$"Survey email for assignment '{assignment.PublicToken}' was queued to '{locationEmail.EmailAddress}'.",
			cancellationToken);

		return result;
	}

	private async Task QueueTenantInvitationEmailAsync(
		string baseUrl,
		string tenantName,
		TenantInvitation invitation,
		string rawToken,
		CancellationToken cancellationToken)
	{
		var invitationUrl = BuildAbsoluteUrl(baseUrl, $"/Account/AcceptInvite?token={Uri.EscapeDataString(rawToken)}");
		var expiresLine = $"This invitation expires {FormatUtc(invitation.ExpiresAtUtc)}.";
		await _queuedEmailService.QueueEmailAsync(
			new QueuedEmailMessage
			{
				TemplateKey = "tenant-invitation",
				SourceType = OutboundEmailSourceTypes.TenantInvitation,
				SourceId = invitation.Id.ToString(),
				RecipientName = invitation.Email,
				RecipientEmail = invitation.Email,
				Subject = $"You're invited to join {tenantName}",
				HtmlBody =
					$"<p>You've been invited to join <strong>{HtmlEncode(tenantName)}</strong> as <strong>{HtmlEncode(invitation.Role.ToString())}</strong>.</p>" +
					$"<p><a href=\"{HtmlEncode(invitationUrl)}\">Create or sign in to your account</a> to accept this invitation.</p>" +
					$"<p>{HtmlEncode(expiresLine)}</p>",
				TextBody =
					$"You've been invited to join {tenantName} as {invitation.Role}.{Environment.NewLine}" +
					$"Accept this invitation: {invitationUrl}{Environment.NewLine}" +
					$"{expiresLine}",
				BaseUrl = NormalizeBaseUrl(baseUrl),
				TenantId = invitation.TenantId,
				RequestedByUserId = invitation.CreatedByUserId
			},
			cancellationToken);
	}

	private async Task QueuePlatformInvitationEmailAsync(
		string baseUrl,
		PlatformUserInvitation invitation,
		string rawToken,
		CancellationToken cancellationToken)
	{
		var invitationUrl = BuildAbsoluteUrl(baseUrl, $"/Account/AcceptPlatformInvite?token={Uri.EscapeDataString(rawToken)}");
		var accessSummary = invitation.IsPlatformSuperAdmin
			? "full platform administration"
			: invitation.IsPlatformUserEnabled
				? "platform access with the selected permissions"
				: "a pending platform invitation";
		var tenantSummary = invitation.TenantId.HasValue && invitation.Tenant is not null
			? $" You will also be added to tenant '{invitation.Tenant.Name}' as {invitation.TenantRole ?? TenantRole.User}."
			: string.Empty;
		var expiresLine = $"This invitation expires {FormatUtc(invitation.ExpiresAtUtc)}.";
		var tenantSummaryHtml = string.IsNullOrWhiteSpace(tenantSummary)
			? string.Empty
			: $"<p>{HtmlEncode(tenantSummary.Trim())}</p>";
		await _queuedEmailService.QueueEmailAsync(
			new QueuedEmailMessage
			{
				TemplateKey = "platform-invitation",
				SourceType = OutboundEmailSourceTypes.PlatformInvitation,
				SourceId = invitation.Id.ToString(),
				RecipientName = invitation.Email,
				RecipientEmail = invitation.Email,
				Subject = "You're invited to access Survey",
				HtmlBody =
					$"<p>You've been invited to access <strong>Survey</strong> with <strong>{HtmlEncode(accessSummary)}</strong>.</p>" +
					tenantSummaryHtml +
					$"<p><a href=\"{HtmlEncode(invitationUrl)}\">Create or sign in to your account</a> to accept this invitation.</p>" +
					$"<p>{HtmlEncode(expiresLine)}</p>",
				TextBody =
					$"You've been invited to access Survey with {accessSummary}.{tenantSummary}{Environment.NewLine}" +
					$"Accept this invitation: {invitationUrl}{Environment.NewLine}" +
					$"{expiresLine}",
				BaseUrl = NormalizeBaseUrl(baseUrl),
				TenantId = invitation.TenantId,
				RequestedByUserId = invitation.CreatedByUserId
			},
			cancellationToken);
	}

	private static string NormalizeBaseUrl(string baseUrl)
	{
		if (!Uri.TryCreate(baseUrl?.Trim(), UriKind.Absolute, out var absoluteUri))
		{
			throw new InvalidOperationException("A valid absolute base URL is required.");
		}

		return absoluteUri.GetLeftPart(UriPartial.Authority);
	}

	private static string BuildAbsoluteUrl(string baseUrl, string relativePath)
	{
		var root = new Uri($"{NormalizeBaseUrl(baseUrl).TrimEnd('/')}/", UriKind.Absolute);
		return new Uri(root, relativePath.TrimStart('/')).AbsoluteUri;
	}

	private static string FormatUtc(DateTimeOffset value)
	{
		return $"{value.UtcDateTime:f} UTC";
	}

	private static string HtmlEncode(string? value)
	{
		return WebUtility.HtmlEncode(value ?? string.Empty);
	}
}
