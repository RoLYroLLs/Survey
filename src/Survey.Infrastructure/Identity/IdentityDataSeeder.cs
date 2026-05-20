using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Identity;

public sealed class IdentityDataSeeder(
	RoleManager<IdentityRole> roleManager,
	UserManager<ApplicationUser> userManager,
	ILogger<IdentityDataSeeder> logger)
{
	private readonly string[] _roles = [RoleNames.Admin, RoleNames.Employee, RoleNames.PlatformSuperAdmin];

	public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
	{
		var existingRoles = await roleManager.Roles
			.Select(role => role.Name)
			.Where(name => name != null)
			.ToListAsync(cancellationToken);
		var existingLookup = existingRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return _roles.All(existingLookup.Contains);
	}

	public async Task SeedAsync(
		Func<InitialSeedingProgressUpdate, Task>? reportProgress = null,
		CancellationToken cancellationToken = default)
	{
		var total = _roles.Length;
		var processed = 0;
		await ReportProgressAsync(reportProgress, processed, total, isComplete: false, "Preparing platform roles.");

		foreach (var roleName in _roles)
		{
			var activityMessage = $"Adding role '{roleName}'.";
			if (!await roleManager.RoleExistsAsync(roleName))
			{
				var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
				if (!createRoleResult.Succeeded)
				{
					throw new InvalidOperationException($"Unable to create role '{roleName}': {string.Join("; ", createRoleResult.Errors.Select(static error => error.Description))}");
				}
			}

			processed++;
			await ReportProgressAsync(reportProgress, processed, total, isComplete: processed == total, activityMessage);
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

	private static Task ReportProgressAsync(
		Func<InitialSeedingProgressUpdate, Task>? reportProgress,
		int processed,
		int total,
		bool isComplete,
		string activityMessage)
	{
		if (reportProgress is null)
		{
			return Task.CompletedTask;
		}

		return reportProgress(new InitialSeedingProgressUpdate
		{
			StageKey = InitialSeedingStages.Roles,
			StageLabel = InitialSeedingStages.GetLabel(InitialSeedingStages.Roles),
			ActivityMessage = activityMessage,
			Processed = processed,
			Total = total,
			IsComplete = isComplete
		});
	}
}
