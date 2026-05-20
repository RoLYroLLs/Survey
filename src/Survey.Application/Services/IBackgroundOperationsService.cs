using Survey.Application.Models;

namespace Survey.Application.Services;

public interface IBackgroundOperationsService
{
	Task<PagedResult<BackgroundOperationListItem>> GetBackgroundOperationsAsync(PagedQuery request, string? kind = null, string? status = null, int? tenantId = null, string? search = null, CancellationToken cancellationToken = default);
	Task<BackgroundOperationDetailModel?> GetBackgroundOperationAsync(int id, CancellationToken cancellationToken = default);
	Task<PagedResult<OutboundEmailListItem>> GetOutboundEmailsAsync(PagedQuery request, string? status = null, int? tenantId = null, string? sourceType = null, string? search = null, CancellationToken cancellationToken = default);
	Task<OutboundEmailDetailModel?> GetOutboundEmailAsync(int id, CancellationToken cancellationToken = default);
	Task<InitialSetupJobStatusModel> GetInitialSetupJobStatusAsync(CancellationToken cancellationToken = default);
}
