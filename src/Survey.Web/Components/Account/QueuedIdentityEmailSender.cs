using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Identity;

namespace Survey.Web.Components.Account;

internal sealed class QueuedIdentityEmailSender(IQueuedEmailService queuedEmailService, IPublicOriginResolver publicOriginResolver) : IEmailSender<ApplicationUser>
{
	private readonly IQueuedEmailService _queuedEmailService = queuedEmailService;
	private readonly IPublicOriginResolver _publicOriginResolver = publicOriginResolver;

	public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
	{
		var baseUrl = BuildBaseUrl(confirmationLink);
		await _queuedEmailService.QueueEmailAsync(new QueuedEmailMessage
		{
			TemplateKey = "identity-confirm-email",
			SourceType = OutboundEmailSourceTypes.IdentityConfirmation,
			SourceId = user.Id,
			RecipientName = BuildRecipientName(user),
			RecipientEmail = email,
			Subject = "Confirm your email",
			HtmlBody = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.",
			TextBody = $"Please confirm your account by visiting: {confirmationLink}",
			BaseUrl = baseUrl
		});
	}

	public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
	{
		var baseUrl = BuildBaseUrl(resetLink);
		await _queuedEmailService.QueueEmailAsync(new QueuedEmailMessage
		{
			TemplateKey = "identity-reset-password-link",
			SourceType = OutboundEmailSourceTypes.IdentityPasswordReset,
			SourceId = user.Id,
			RecipientName = BuildRecipientName(user),
			RecipientEmail = email,
			Subject = "Reset your password",
			HtmlBody = $"Please reset your password by <a href='{resetLink}'>clicking here</a>.",
			TextBody = $"Please reset your password by visiting: {resetLink}",
			BaseUrl = baseUrl
		});
	}

	public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
	{
		var baseUrl = _publicOriginResolver.ResolvePublicOrigin();
		await _queuedEmailService.QueueEmailAsync(new QueuedEmailMessage
		{
			TemplateKey = "identity-reset-password-code",
			SourceType = OutboundEmailSourceTypes.IdentityPasswordReset,
			SourceId = user.Id,
			RecipientName = BuildRecipientName(user),
			RecipientEmail = email,
			Subject = "Reset your password",
			HtmlBody = $"Please reset your password using the following code: <strong>{resetCode}</strong>",
			TextBody = $"Please reset your password using the following code: {resetCode}",
			BaseUrl = baseUrl
		});
	}

	private static string BuildRecipientName(ApplicationUser user)
	{
		var displayName = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
		return string.IsNullOrWhiteSpace(displayName)
			? user.Email ?? user.UserName ?? string.Empty
			: displayName;
	}

	private static string BuildBaseUrl(string url)
	{
		if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
		{
			return absolute.GetLeftPart(UriPartial.Authority);
		}

		throw new InvalidOperationException($"Unable to derive a public origin from URL '{url}'. Use an absolute URL or configure App:PublicOrigin.");
	}
}
