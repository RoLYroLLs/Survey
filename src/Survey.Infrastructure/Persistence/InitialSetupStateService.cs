using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Persistence;

public sealed class InitialSetupStateService(
	IMemoryCache memoryCache,
	IServiceScopeFactory serviceScopeFactory)
{
	private static readonly MemoryCacheEntryOptions CacheOptions = new()
	{
		AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
	};

	private const string CacheKey = "survey.initial-setup-state";
	private readonly IMemoryCache _memoryCache = memoryCache;
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

	public async Task<InitialSetupState> GetStatusAsync(CancellationToken cancellationToken = default)
	{
		if (_memoryCache.TryGetValue<InitialSetupState>(CacheKey, out var cachedState))
		{
			return cachedState;
		}

		var loadedState = await LoadStatusAsync(cancellationToken);
		_memoryCache.Set(CacheKey, loadedState, CacheOptions);
		return loadedState;
	}

	public void SetStatus(bool hasUsers, bool isComplete)
	{
		_memoryCache.Set(CacheKey, new InitialSetupState(hasUsers, isComplete), CacheOptions);
	}

	public void Invalidate()
	{
		_memoryCache.Remove(CacheKey);
	}

	private async Task<InitialSetupState> LoadStatusAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceScopeFactory.CreateScope();
		var serviceProvider = scope.ServiceProvider;
		var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var hasUsers = await userManager.Users.AnyAsync(cancellationToken);
		if (!hasUsers)
		{
			return new InitialSetupState(false, false);
		}

		var identityDataSeeder = serviceProvider.GetRequiredService<IdentityDataSeeder>();
		var siteSettingsSeeder = serviceProvider.GetRequiredService<SiteSettingsSeeder>();
		var geographyDataSeeder = serviceProvider.GetRequiredService<GeographyDataSeeder>();
		var isComplete = await identityDataSeeder.IsSeededAsync(cancellationToken)
			&& await siteSettingsSeeder.IsSeededAsync(cancellationToken)
			&& await geographyDataSeeder.IsSeededAsync(cancellationToken);
		return new InitialSetupState(true, isComplete);
	}
}

public readonly record struct InitialSetupState(bool HasUsers, bool IsComplete)
{
	public bool IsInProgress => HasUsers && !IsComplete;
}
