using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Survey.Application.Services;
using Survey.Infrastructure.Configuration;

namespace Survey.Infrastructure.Services;

internal sealed class PublicOriginResolver(IHttpContextAccessor httpContextAccessor, IOptions<AppOptions> appOptions) : IPublicOriginResolver
{
	private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
	private readonly AppOptions _appOptions = appOptions.Value;

	public string ResolvePublicOrigin()
	{
		var request = _httpContextAccessor.HttpContext?.Request;
		if (request is not null && request.Host.HasValue)
		{
			return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
		}

		var configuredOrigin = _appOptions.PublicOrigin?.Trim();
		if (!string.IsNullOrWhiteSpace(configuredOrigin))
		{
			if (Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var absoluteOrigin))
			{
				return absoluteOrigin.GetLeftPart(UriPartial.Authority).TrimEnd('/') + absoluteOrigin.AbsolutePath.TrimEnd('/');
			}

			throw new InvalidOperationException("App:PublicOrigin must be an absolute URL when configured.");
		}

		throw new InvalidOperationException("A public app origin could not be resolved. Configure App:PublicOrigin for this environment.");
	}
}
