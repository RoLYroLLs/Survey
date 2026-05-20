using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Survey.Domain;

namespace Survey.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
	[StringLength(100)]
	public string? FirstName { get; set; }

	[StringLength(100)]
	public string? LastName { get; set; }

	[StringLength(2000)]
	public string? FavoriteGoalIds { get; set; }

	[StringLength(200)]
	public string? AddressLine1 { get; set; }

	[StringLength(200)]
	public string? AddressLine2 { get; set; }

	[StringLength(100)]
	public string? City { get; set; }

	[StringLength(100)]
	public string? State { get; set; }

	[StringLength(20)]
	public string? PostalCode { get; set; }

	public bool IsOrganizationAccount { get; set; }

	[StringLength(200)]
	public string? OrganizationName { get; set; }

	public int? ActiveTenantMembershipId { get; set; }

	public bool IsPlatformSuperAdmin { get; set; }

	public bool IsPlatformUserEnabled { get; set; }

	public bool IsBootstrapPlatformOwner { get; set; }

	[StringLength(16)]
	public string? AvatarColorHex { get; set; }

	public ICollection<TenantMembership> TenantMemberships { get; } = new List<TenantMembership>();

	public ICollection<PlatformUserPermission> PlatformPermissions { get; } = new List<PlatformUserPermission>();

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

	public bool HasCompletedSelfSignupProfile()
	{
		return !string.IsNullOrWhiteSpace(FirstName)
			&& !string.IsNullOrWhiteSpace(LastName)
			&& (!IsOrganizationAccount || !string.IsNullOrWhiteSpace(OrganizationName))
			&& !string.IsNullOrWhiteSpace(AddressLine1)
			&& !string.IsNullOrWhiteSpace(City)
			&& !string.IsNullOrWhiteSpace(State)
			&& !string.IsNullOrWhiteSpace(PostalCode)
			&& !string.IsNullOrWhiteSpace(PhoneNumber);
	}
}
