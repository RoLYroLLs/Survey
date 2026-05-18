using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Survey.Domain;

namespace Survey.Infrastructure.Identity;

internal sealed class IdentityDataSeeder(
	RoleManager<IdentityRole> roleManager,
	UserManager<ApplicationUser> userManager,
	IConfiguration configuration,
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

		var email = configuration["SeedAdmin:Email"]?.Trim();
		var password = configuration["SeedAdmin:Password"];
		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			logger.LogInformation("Seed admin credentials were not provided. Roles were created, but no admin user was seeded.");
			return;
		}

		var user = await userManager.FindByEmailAsync(email);
		if (user is null)
		{
			user = new ApplicationUser
			{
				UserName = email,
				Email = email,
				EmailConfirmed = true,
				FirstName = configuration["SeedAdmin:FirstName"]?.Trim(),
				LastName = configuration["SeedAdmin:LastName"]?.Trim()
			};
			UserAvatarPalette.EnsureAssigned(user);

			var createUserResult = await userManager.CreateAsync(user, password);
			if (!createUserResult.Succeeded)
			{
				throw new InvalidOperationException($"Unable to create the seed admin user: {string.Join("; ", createUserResult.Errors.Select(static error => error.Description))}");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Admin);
			if (!roleResult.Succeeded)
			{
				throw new InvalidOperationException($"Unable to assign the admin role to '{email}': {string.Join("; ", roleResult.Errors.Select(static error => error.Description))}");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.PlatformSuperAdmin))
		{
			var platformRoleResult = await userManager.AddToRoleAsync(user, RoleNames.PlatformSuperAdmin);
			if (!platformRoleResult.Succeeded)
			{
				throw new InvalidOperationException($"Unable to assign the platform super admin role to '{email}': {string.Join("; ", platformRoleResult.Errors.Select(static error => error.Description))}");
			}
		}

		user.IsPlatformSuperAdmin = true;
		user.IsPlatformUserEnabled = true;
		UserAvatarPalette.EnsureAssigned(user);
		await userManager.UpdateAsync(user);

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
