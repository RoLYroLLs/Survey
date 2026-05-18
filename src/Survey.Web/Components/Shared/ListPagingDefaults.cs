using Survey.Application.Models;

namespace Survey.Web.Components.Shared;

public static class ListPagingDefaults
{
	public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

	public static int NormalizePageSize(int? pageSize)
	{
		return new PagedQuery
		{
			Limit = pageSize ?? PagedQuery.DefaultLimit
		}.Normalize().Limit;
	}

	public static PagedQuery CreateRequest(int pageSize, int offset = 0)
	{
		return new PagedQuery
		{
			Offset = Math.Max(0, offset),
			Limit = NormalizePageSize(pageSize)
		};
	}

	public static PagedQuery CreateRequest(int pageSize, int offset, string? sort)
	{
		return new PagedQuery
		{
			Offset = Math.Max(0, offset),
			Limit = NormalizePageSize(pageSize),
			Sort = PagingSort.Serialize(PagingSort.Parse(sort))
		};
	}
}
