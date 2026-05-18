namespace Survey.Application.Models;

public sealed class TenantSearchResultModel
{
	public string Query { get; init; } = string.Empty;
	public IReadOnlyList<TenantSearchSectionModel> Sections { get; init; } = Array.Empty<TenantSearchSectionModel>();
}

public sealed class TenantSearchSectionModel
{
	public string Key { get; init; } = string.Empty;
	public string Title { get; init; } = string.Empty;
	public string? ViewAllUrl { get; init; }
	public int TotalCount { get; init; }
	public IReadOnlyList<TenantSearchItemModel> Items { get; init; } = Array.Empty<TenantSearchItemModel>();
}

public sealed class TenantSearchItemModel
{
	public string Title { get; init; } = string.Empty;
	public string? Subtitle { get; init; }
	public string Url { get; init; } = string.Empty;
	public string IconName { get; init; } = string.Empty;
}
