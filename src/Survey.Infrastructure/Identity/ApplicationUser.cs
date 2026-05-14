using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Survey.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
	[StringLength(100)]
	public string? FirstName { get; set; }

	[StringLength(100)]
	public string? LastName { get; set; }

	[StringLength(2000)]
	public string? FavoriteGoalIds { get; set; }

	public string DisplayName
	{
		get
		{
			var parts = new[] { FirstName?.Trim(), LastName?.Trim() }
				.Where(static value => !string.IsNullOrWhiteSpace(value))
				.ToArray();

			return parts.Length > 0 ? string.Join(" ", parts) : (Email ?? UserName ?? "Unknown user");
		}
	}

	public HashSet<int> GetFavoriteGoalIds()
	{
		return ParseFavoriteGoalIds(FavoriteGoalIds);
	}

	public void SetFavoriteGoalIds(IEnumerable<int> goalIds)
	{
		FavoriteGoalIds = string.Join(",",
			goalIds
				.Where(static goalId => goalId > 0)
				.Distinct()
				.OrderBy(static goalId => goalId));
	}

	public static HashSet<int> ParseFavoriteGoalIds(string? serializedValue)
	{
		if (string.IsNullOrWhiteSpace(serializedValue))
		{
			return [];
		}

		return serializedValue
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(static value => int.TryParse(value, out var parsed) ? parsed : 0)
			.Where(static value => value > 0)
			.ToHashSet();
	}
}
