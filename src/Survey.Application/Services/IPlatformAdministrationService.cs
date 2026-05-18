using Survey.Application.Models;

namespace Survey.Application.Services;

public interface IPlatformAdministrationService
{
	Task<IReadOnlyList<CountryListItem>> GetCountriesAsync(string? search = null, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<SelectOption>> GetStateProvinceSelectOptionsAsync(int? countryId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<SelectOption>> GetCountySelectOptionsAsync(int? stateProvinceId, CancellationToken cancellationToken = default);
	Task<CountryEditModel> GetCountryAsync(int? id, CancellationToken cancellationToken = default);
	Task<int> SaveCountryAsync(CountryEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportCountriesAsync(CountryImportModel model, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<StateProvinceListItem>> GetStateProvincesAsync(int? countryId = null, string? search = null, CancellationToken cancellationToken = default);
	Task<StateProvinceEditModel> GetStateProvinceAsync(int? id, int? countryId, CancellationToken cancellationToken = default);
	Task<int> SaveStateProvinceAsync(StateProvinceEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportStateProvincesAsync(StateProvinceImportModel model, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<CountyListItem>> GetCountiesAsync(int? stateProvinceId = null, string? search = null, CancellationToken cancellationToken = default);
	Task<CountyEditModel> GetCountyAsync(int? id, int? stateProvinceId, CancellationToken cancellationToken = default);
	Task<int> SaveCountyAsync(CountyEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportCountiesAsync(CountyImportModel model, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<PostalAddressListItem>> GetPostalAddressesAsync(string? search = null, int? countyId = null, CancellationToken cancellationToken = default);
	Task<PostalAddressEditModel> GetPostalAddressAsync(int? id, CancellationToken cancellationToken = default);
	Task<PostalAddressReferenceViewModel> GetPostalAddressReferencesAsync(int id, CancellationToken cancellationToken = default);
	Task<int> SavePostalAddressAsync(PostalAddressEditModel model, CancellationToken cancellationToken = default);
	Task<GeographyImportResultModel> ImportPostalAddressesAsync(PostalAddressImportModel model, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ZipCountyMappingListItem>> GetZipCountyMappingsAsync(string? search = null, CancellationToken cancellationToken = default);
	Task<ZipCountyMappingEditModel> GetZipCountyMappingAsync(int? id, CancellationToken cancellationToken = default);
	Task<int> SaveZipCountyMappingAsync(ZipCountyMappingEditModel model, CancellationToken cancellationToken = default);
	Task<ZipCountyImportResultModel> ImportZipCountyMappingsAsync(ZipCountyImportModel model, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<PlatformUserListItem>> GetPlatformUsersAsync(string? search = null, CancellationToken cancellationToken = default);
	Task<PlatformUserEditModel> GetPlatformUserAsync(string? id, CancellationToken cancellationToken = default);
	Task<string> SavePlatformUserAsync(PlatformUserEditModel model, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<PlatformTenantListItem>> GetPlatformTenantsAsync(string? search = null, CancellationToken cancellationToken = default);
	Task<PlatformTenantDetailModel> GetPlatformTenantAsync(int tenantId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<AuditLogListItem>> GetAuditLogsAsync(string? plane = null, int? tenantId = null, bool? succeeded = null, string? search = null, int take = 200, CancellationToken cancellationToken = default);
}
