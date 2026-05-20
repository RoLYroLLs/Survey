using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

public sealed class EmailTrackingService(SurveyDbContext dbContext) : IEmailTrackingService
{
	private readonly SurveyDbContext _dbContext = dbContext;

	public async Task TrackOpenAsync(string token, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return;
		}

		var email = await _dbContext.OutboundEmails
			.FirstOrDefaultAsync(item => item.TrackingToken == token.Trim(), cancellationToken);
		if (email is null)
		{
			return;
		}

		email.RecordOpen();
		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	public async Task<EmailClickRedirectResult> TrackClickAsync(string token, string? linkType, string? destinationUrl, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(destinationUrl))
		{
			return new EmailClickRedirectResult();
		}

		var normalizedToken = token.Trim();
		var normalizedDestination = destinationUrl.Trim();
		if (!IsAllowedDestination(normalizedDestination))
		{
			return new EmailClickRedirectResult();
		}

		var email = await _dbContext.OutboundEmails
			.FirstOrDefaultAsync(item => item.TrackingToken == normalizedToken, cancellationToken);
		if (email is null)
		{
			return new EmailClickRedirectResult();
		}

		email.RecordClick();
		_dbContext.OutboundEmailClickEvents.Add(new OutboundEmailClickEvent(
			email.Id,
			string.IsNullOrWhiteSpace(linkType) ? "link" : linkType.Trim(),
			normalizedDestination,
			userAgent,
			HashIpAddress(ipAddress)));
		await _dbContext.SaveChangesAsync(cancellationToken);

		return new EmailClickRedirectResult
		{
			IsValid = true,
			DestinationUrl = normalizedDestination
		};
	}

	private static string HashIpAddress(string? ipAddress)
	{
		if (string.IsNullOrWhiteSpace(ipAddress))
		{
			return string.Empty;
		}

		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress.Trim())));
	}

	private static bool IsAllowedDestination(string destinationUrl)
	{
		if (destinationUrl.StartsWith("/", StringComparison.Ordinal))
		{
			return true;
		}

		return Uri.TryCreate(destinationUrl, UriKind.Absolute, out var uri)
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	}
}
