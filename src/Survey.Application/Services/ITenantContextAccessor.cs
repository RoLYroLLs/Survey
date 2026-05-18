using Survey.Application.Models;

namespace Survey.Application.Services;

public interface ITenantContextAccessor
{
	Task<CurrentAccessContext> GetCurrentAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<TenantMembershipOption>> GetMembershipOptionsAsync(CancellationToken cancellationToken = default);
	Task<CurrentAccessContext> RequireTenantContextAsync(CancellationToken cancellationToken = default);
	Task<CurrentAccessContext> RequirePlatformContextAsync(CancellationToken cancellationToken = default);
	Task SwitchActiveTenantAsync(int membershipId, CancellationToken cancellationToken = default);
}
