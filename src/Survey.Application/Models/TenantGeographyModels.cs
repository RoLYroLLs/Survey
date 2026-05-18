namespace Survey.Application.Models;

public sealed class TenantGeographyVisibilityEditModel
{
	public List<int> VisibleCountryIds { get; set; } = [];
	public List<int> VisibleStateProvinceIds { get; set; } = [];
	public List<int> VisibleCountyIds { get; set; } = [];
	public IReadOnlyList<SelectOption> CountryOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> StateProvinceOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> CountyOptions { get; set; } = Array.Empty<SelectOption>();
}
