namespace Survey.Application.Models;

public enum SortDirection
{
	Ascending,
	Descending
}

public sealed class SortDescriptor
{
	public string Key { get; set; } = string.Empty;
	public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

public static class PagingSort
{
	public const int MaxColumns = 3;

	public static IReadOnlyList<SortDescriptor> Parse(string? sort)
	{
		if (string.IsNullOrWhiteSpace(sort))
		{
			return [];
		}

		var descriptors = new List<SortDescriptor>();
		var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var segment in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
			var key = parts[0].Trim();
			if (string.IsNullOrWhiteSpace(key) || !seenKeys.Add(key))
			{
				continue;
			}

			descriptors.Add(new SortDescriptor
			{
				Key = key,
				Direction = parts.Length > 1 && string.Equals(parts[1], "desc", StringComparison.OrdinalIgnoreCase)
					? SortDirection.Descending
					: SortDirection.Ascending
			});

			if (descriptors.Count >= MaxColumns)
			{
				break;
			}
		}

		return descriptors;
	}

	public static string? Serialize(IEnumerable<SortDescriptor> sorts)
	{
		var serialized = sorts
			.Where(sort => !string.IsNullOrWhiteSpace(sort.Key))
			.Take(MaxColumns)
			.Select(sort => $"{sort.Key.Trim()}:{(sort.Direction == SortDirection.Descending ? "desc" : "asc")}")
			.ToArray();

		return serialized.Length == 0 ? null : string.Join(",", serialized);
	}

	public static string? Toggle(string? currentSort, string key, int maxColumns = MaxColumns)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return Serialize(Parse(currentSort));
		}

		var normalizedKey = key.Trim();
		var descriptors = Parse(currentSort).ToList();
		var existingIndex = descriptors.FindIndex(sort => string.Equals(sort.Key, normalizedKey, StringComparison.OrdinalIgnoreCase));

		if (existingIndex < 0)
		{
			descriptors.Insert(0, new SortDescriptor
			{
				Key = normalizedKey,
				Direction = SortDirection.Ascending
			});
		}
		else if (existingIndex == 0)
		{
			if (descriptors[0].Direction == SortDirection.Ascending)
			{
				descriptors[0].Direction = SortDirection.Descending;
			}
			else
			{
				descriptors.RemoveAt(0);
			}
		}
		else
		{
			var descriptor = descriptors[existingIndex];
			descriptors.RemoveAt(existingIndex);
			descriptors.Insert(0, descriptor);
		}

		return Serialize(descriptors.Take(maxColumns));
	}

	public static SortDescriptor? Get(string? currentSort, string key)
	{
		return Parse(currentSort)
			.FirstOrDefault(sort => string.Equals(sort.Key, key, StringComparison.OrdinalIgnoreCase));
	}

	public static int GetPriority(string? currentSort, string key)
	{
		var descriptors = Parse(currentSort);
		for (var index = 0; index < descriptors.Count; index++)
		{
			if (string.Equals(descriptors[index].Key, key, StringComparison.OrdinalIgnoreCase))
			{
				return index + 1;
			}
		}

		return 0;
	}
}

public sealed class PagedQuery
{
	public const int DefaultLimit = 10;
	public const int MaxLimit = 100;

	public int Offset { get; set; }
	public int Limit { get; set; } = DefaultLimit;
	public string? Sort { get; set; }

	public PagedQuery Normalize()
	{
		return new PagedQuery
		{
			Offset = Math.Max(0, Offset),
			Limit = Limit <= 0
				? DefaultLimit
				: Math.Min(Limit, MaxLimit),
			Sort = PagingSort.Serialize(PagingSort.Parse(Sort))
		};
	}
}

public sealed class PagedResult<T>
{
	public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
	public int TotalCount { get; set; }
	public bool HasMore { get; set; }
}
