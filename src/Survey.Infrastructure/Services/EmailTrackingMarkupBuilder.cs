using System.Text.RegularExpressions;

namespace Survey.Infrastructure.Services;

internal static partial class EmailTrackingMarkupBuilder
{
	public static string AttachTracking(string htmlBody, string baseUrl, string trackingToken)
	{
		if (string.IsNullOrWhiteSpace(htmlBody) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(trackingToken))
		{
			return htmlBody;
		}

		var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/');
		var normalizedToken = Uri.EscapeDataString(trackingToken.Trim());
		var rewritten = HrefRegex().Replace(htmlBody, match =>
		{
			var quote = match.Groups["quote"].Value;
			var href = match.Groups["href"].Value;
			if (string.IsNullOrWhiteSpace(href))
			{
				return match.Value;
			}

			var linkType = ClassifyLinkType(href);
			var trackedUrl = $"{normalizedBaseUrl}/email/track/click/{normalizedToken}?url={Uri.EscapeDataString(href)}&kind={Uri.EscapeDataString(linkType)}";
			return $"href={quote}{trackedUrl}{quote}";
		});

		var pixelMarkup = $"<img src=\"{normalizedBaseUrl}/email/track/open/{normalizedToken}\" alt=\"\" width=\"1\" height=\"1\" style=\"display:block;width:1px;height:1px;border:0;opacity:0;\" />";
		return rewritten.Contains("</body>", StringComparison.OrdinalIgnoreCase)
			? Regex.Replace(rewritten, "</body>", $"{pixelMarkup}</body>", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))
			: $"{rewritten}{pixelMarkup}";
	}

	private static string ClassifyLinkType(string href)
	{
		if (href.Contains("/Account/ConfirmEmail", StringComparison.OrdinalIgnoreCase))
		{
			return "confirm-email";
		}

		if (href.Contains("/Account/ResetPassword", StringComparison.OrdinalIgnoreCase))
		{
			return "reset-password";
		}

		if (href.Contains("/Account/AcceptInvite", StringComparison.OrdinalIgnoreCase))
		{
			return "tenant-invite";
		}

		if (href.Contains("/Account/AcceptPlatformInvite", StringComparison.OrdinalIgnoreCase))
		{
			return "platform-invite";
		}

		if (href.Contains("/survey/", StringComparison.OrdinalIgnoreCase))
		{
			return "survey";
		}

		return "link";
	}

	[GeneratedRegex("href=(?<quote>['\"])(?<href>[^'\"]+)(\\k<quote>)", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex HrefRegex();
}
