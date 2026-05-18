using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Survey.Domain;

namespace Survey.Infrastructure.Identity;

internal sealed class IdentityDataSeeder(
	RoleManager<IdentityRole> roleManager,
	UserManager<ApplicationUser> userManager,
	ILogger<IdentityDataSeeder> logger)
{
	private readonly string[] _roles = [RoleNames.Admin, RoleNames.Employee, RoleNames.PlatformSuperAdmin];

	public async Task SeedAsync(CancellationToken cancellationToken = default)
	{
		foreach (var roleName in _roles)
		{
			if (!await roleManager.RoleExistsAsync(roleName))
			{
				var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
				if (!createRoleResult.Succeeded)
				{
					throw new InvalidOperationException($"Unable to create role '{roleName}': {string.Join("; ", createRoleResult.Errors.Select(static error => error.Description))}");
				}
			}
		}

		logger.LogInformation("Identity roles were verified. No platform admin is seeded from configuration.");

		var usersMissingAvatarColor = await userManager.Users
			.Where(existingUser => existingUser.AvatarColorHex == null || existingUser.AvatarColorHex == string.Empty)
			.ToListAsync(cancellationToken);
		foreach (var existingUser in usersMissingAvatarColor)
		{
			UserAvatarPalette.EnsureAssigned(existingUser);
			await userManager.UpdateAsync(existingUser);
		}
	}
}
