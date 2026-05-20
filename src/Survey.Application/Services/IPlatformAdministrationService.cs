using Survey.Application.Models;

namespace Survey.Application.Services;

public interface IPlatformAdministrationService
{
	Task<PagedResult<CountryListItem>> GetCountriesAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<SelectOption>> GetStateProvinceSelectOptionsAsync(int? countryId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<SelectOption>> GetCountySelectOptionsAsync(int? stateProvinceId, CancellationToken cancellationToken = default);
	Task<CountryEditModel> GetCountryAsync(int? id, CancellationToken cancellationToken = default);
	Task<int> SaveCountryAsync(CountryEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportCountriesAsync(CountryImportModel model, CancellationToken cancellationToken = default);
	Task<PagedResult<StateProvinceListItem>> GetStateProvincesAsync(PagedQuery request, int? countryId = null, string? search = null, CancellationToken cancellationToken = default);
	Task<StateProvinceEditModel> GetStateProvinceAsync(int? id, int? countryId, CancellationToken cancellationToken = default);
	Task<int> SaveStateProvinceAsync(StateProvinceEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportStateProvincesAsync(StateProvinceImportModel model, CancellationToken cancellationToken = default);
	Task<PagedResult<CountyListItem>> GetCountiesAsync(PagedQuery request, int? stateProvinceId = null, string? search = null, CancellationToken cancellationToken = default);
	Task<CountyEditModel> GetCountyAsync(int? id, int? stateProvinceId, CancellationToken cancellationToken = default);
	Task<int> SaveCountyAsync(CountyEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportCountiesAsync(CountyImportModel model, CancellationToken cancellationToken = default);
	Task<PagedResult<PostalAddressListItem>> GetPostalAddressesAsync(PagedQuery request, string? search = null, int? countyId = null, CancellationToken cancellationToken = default);
	Task<PostalAddressEditModel> GetPostalAddressAsync(int? id, CancellationToken cancellationToken = default);
	Task<PostalAddressReferenceViewModel> GetPostalAddressReferencesAsync(int id, CancellationToken cancellationToken = default);
	Task<int> SavePostalAddressAsync(PostalAddressEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportPostalAddressesAsync(PostalAddressImportModel model, CancellationToken cancellationToken = default);
	Task<PagedResult<ZipCountyMappingListItem>> GetZipCountyMappingsAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default);
	Task<ZipCountyMappingEditModel> GetZipCountyMappingAsync(int? id, CancellationToken cancellationToken = default);
	Task<int> SaveZipCountyMappingAsync(ZipCountyMappingEditModel model, CancellationToken cancellationToken = default);
	Task<ZipCountyImportResultModel> ImportZipCountyMappingsAsync(ZipCountyImportModel model, CancellationToken cancellationToken = default);
	Task<PagedResult<PlatformUserListItem>> GetPlatformUsersAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<SelectOption>> GetPlatformTenantSelectOptionsAsync(CancellationToken cancellationToken = default);
	Task<PlatformUserEditModel> GetPlatformUserAsync(string? id, CancellationToken cancellationToken = default);
	Task<string> SavePlatformUserAsync(PlatformUserEditModel model, CancellationToken cancellationToken = default);
	Task<PlatformUserInviteModel> GetPlatformUserInviteAsync(CancellationToken cancellationToken = default);
	Task<PlatformUserInviteResultModel> CreatePlatformUserInvitationAsync(PlatformUserInviteModel model, string baseUrl, CancellationToken cancellationToken = default);
	Task<PlatformUserInvitationAcceptanceContextModel> GetPlatformUserInvitationAcceptanceAsync(string token, CancellationToken cancellationToken = default);
	Task<string> AcceptPlatformUserInvitationAsync(string token, string userId, CancellationToken cancellationToken = default);
	Task<PagedResult<PlatformThemeListItem>> GetPlatformThemesAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default);
	Task<PlatformThemeEditModel> GetPlatformThemeAsync(int? id, CancellationToken cancellationToken = default);
	Task<int> SavePlatformThemeAsync(PlatformThemeEditModel model, CancellationToken cancellationToken = default);
	Task SetPlatformThemeEnabledAsync(int id, bool isEnabled, int? replacementThemeId = null, CancellationToken cancellationToken = default);
	Task SetPlatformThemeArchivedAsync(int id, bool isArchived, CancellationToken cancellationToken = default);
	Task DeletePlatformThemeAsync(int id, int? replacementThemeId = null, CancellationToken cancellationToken = default);
	Task<PagedResult<PlatformTenantListItem>> GetPlatformTenantsAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default);
	Task<PlatformTenantDetailModel> GetPlatformTenantAsync(int tenantId, CancellationToken cancellationToken = default);
	Task<PlatformTenantEditModel> GetPlatformTenantEditAsync(int tenantId, CancellationToken cancellationToken = default);
	Task SavePlatformTenantAsync(PlatformTenantEditModel model, CancellationToken cancellationToken = default);
	Task<PagedResult<AuditLogListItem>> GetAuditLogsAsync(PagedQuery request, string? plane = null, int? tenantId = null, bool? succeeded = null, string? search = null, CancellationToken cancellationToken = default);
}
