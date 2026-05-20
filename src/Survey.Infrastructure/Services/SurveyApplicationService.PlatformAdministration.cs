using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Survey.Application.Models;
using Survey.Domain;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<PagedResult<PlatformUserListItem>> GetPlatformUsersAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersView, cancellationToken);

		var users = await _userManager.Users
			.AsNoTracking()
			.Include(user => user.PlatformPermissions)
			.OrderBy(user => user.LastName)
			.ThenBy(user => user.FirstName)
			.ThenBy(user => user.Email)
			.ToListAsync(cancellationToken);

		var items = users
			.Where(user => string.IsNullOrWhiteSpace(search) || MatchesPlatformUserSearch(user, search!))
			.Select(user => new PlatformUserListItem
			{
				Id = user.Id,
				FirstName = user.FirstName ?? string.Empty,
				LastName = user.LastName ?? string.Empty,
				Email = user.Email ?? string.Empty,
				IsPlatformUserEnabled = user.IsPlatformUserEnabled,
				IsPlatformSuperAdmin = user.IsPlatformSuperAdmin,
				PermissionCount = user.IsPlatformSuperAdmin
					? PlatformPermissionCatalog.All.Count
					: user.PlatformPermissions.Count
			})
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["name"] = [nameof(PlatformUserListItem.LastName), nameof(PlatformUserListItem.FirstName)],
			["email"] = [nameof(PlatformUserListItem.Email)],
			["status"] = [nameof(PlatformUserListItem.IsPlatformUserEnabled)],
			["scope"] = [nameof(PlatformUserListItem.IsPlatformSuperAdmin)],
			["permissions"] = [nameof(PlatformUserListItem.PermissionCount)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(PlatformUserListItem.Id)).ToList();
		var normalizedRequest = NormalizePagedQuery(request);
		var totalCount = orderedItems.Count;
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, totalCount, normalizedRequest.Offset);
	}

	public async Task<IReadOnlyList<SelectOption>> GetPlatformTenantSelectOptionsAsync(CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersManage, cancellationToken);

		return await _dbContext.Tenants
			.AsNoTracking()
			.OrderBy(tenant => tenant.Name)
			.ThenBy(tenant => tenant.Id)
			.Select(tenant => new SelectOption
			{
				Value = tenant.Id.ToString(),
				Label = tenant.Name
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<PlatformUserEditModel> GetPlatformUserAsync(string? id, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersView, cancellationToken);

		var currentUserId = await RequireCurrentUserIdAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(id))
		{
			return BuildPlatformUserEditModel(null, currentUserId);
		}

		var user = await _userManager.Users
			.AsNoTracking()
			.Include(item => item.PlatformPermissions)
			.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested platform user was not found.");

		return BuildPlatformUserEditModel(user, currentUserId);
	}

	public async Task<PlatformUserInviteModel> GetPlatformUserInviteAsync(CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersManage, cancellationToken);

		return new PlatformUserInviteModel
		{
			IsPlatformUserEnabled = true,
			Permissions = PlatformPermissionCatalog.All
				.Select(definition => new PlatformUserPermissionEditModel
				{
					PermissionKey = definition.Key,
					Category = definition.Category,
					PermissionLabel = definition.Label,
					Selected = false
				})
				.ToList()
		};
	}

	public async Task<string> SavePlatformUserAsync(PlatformUserEditModel model, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersManage, cancellationToken);

		var actor = await RequirePlatformAccessAsync(cancellationToken);
		var desiredPermissionKeys = model.Permissions
			.Where(permission => permission.Selected)
			.Select(permission => permission.PermissionKey)
			.Distinct(StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
		var requiresPermissionManagement = model.IsPlatformSuperAdmin || desiredPermissionKeys.Count > 0;
		if (requiresPermissionManagement)
		{
			await RequirePlatformPermissionAsync(PlatformPermissionKeys.PermissionsManage, cancellationToken);
		}

		ApplicationUser user;
		bool isNewUser;
		bool existingEnabled;
		bool existingSuperAdmin;
		HashSet<string> existingPermissions;

		if (string.IsNullOrWhiteSpace(model.Id))
		{
			isNewUser = true;
			existingEnabled = false;
			existingSuperAdmin = false;
			existingPermissions = new HashSet<string>(StringComparer.Ordinal);

			if (string.IsNullOrWhiteSpace(model.Password))
			{
				throw new InvalidOperationException("A password is required when creating a new platform user.");
			}

			user = new ApplicationUser
			{
				UserName = model.Email.Trim(),
				Email = model.Email.Trim(),
				EmailConfirmed = true,
				FirstName = model.FirstName.Trim(),
				LastName = model.LastName.Trim(),
				IsPlatformUserEnabled = model.IsPlatformUserEnabled,
				IsPlatformSuperAdmin = model.IsPlatformSuperAdmin
			};
			UserAvatarPalette.EnsureAssigned(user);

			var createResult = await _userManager.CreateAsync(user, model.Password);
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(static error => error.Description)));
			}
		}
		else
		{
			isNewUser = false;
			user = await _userManager.Users
				.Include(item => item.PlatformPermissions)
				.FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken)
				?? throw new InvalidOperationException("The requested platform user was not found.");

			existingEnabled = user.IsPlatformUserEnabled;
			existingSuperAdmin = user.IsPlatformSuperAdmin;
			existingPermissions = user.PlatformPermissions
				.Select(permission => permission.PermissionKey)
				.ToHashSet(StringComparer.Ordinal);

			var accessStateChanged = existingEnabled != model.IsPlatformUserEnabled
				|| existingSuperAdmin != model.IsPlatformSuperAdmin
				|| !existingPermissions.SetEquals(desiredPermissionKeys);
			if (user.IsBootstrapPlatformOwner && accessStateChanged)
			{
				throw new InvalidOperationException("The bootstrap platform owner cannot be disabled or have its platform role changed.");
			}
			if (string.Equals(user.Id, actor.UserId, StringComparison.Ordinal) && accessStateChanged)
			{
				throw new InvalidOperationException("You cannot change your own platform access, role, or permissions.");
			}

			user.UserName = model.Email.Trim();
			user.Email = model.Email.Trim();
			user.FirstName = model.FirstName.Trim();
			user.LastName = model.LastName.Trim();
			user.IsPlatformUserEnabled = model.IsPlatformUserEnabled;
			user.IsPlatformSuperAdmin = model.IsPlatformSuperAdmin;

			var updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(static error => error.Description)));
			}
		}

		if (!isNewUser)
		{
			if (!string.IsNullOrWhiteSpace(model.Password))
			{
				if (await _userManager.HasPasswordAsync(user))
				{
					var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
					var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
					if (!resetResult.Succeeded)
					{
						throw new InvalidOperationException(string.Join("; ", resetResult.Errors.Select(static error => error.Description)));
					}
				}
				else
				{
					var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
					if (!addPasswordResult.Succeeded)
					{
						throw new InvalidOperationException(string.Join("; ", addPasswordResult.Errors.Select(static error => error.Description)));
					}
				}
			}
		}

		await ApplyPlatformUserPermissionsAsync(user, model.IsPlatformSuperAdmin ? [] : desiredPermissionKeys, cancellationToken);
		await EnsurePlatformUserGuardrailsAsync(user.Id, model.IsPlatformUserEnabled, model.IsPlatformSuperAdmin, model.IsPlatformSuperAdmin ? [] : desiredPermissionKeys, cancellationToken);

		var saveResult = await _userManager.UpdateAsync(user);
		if (!saveResult.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", saveResult.Errors.Select(static error => error.Description)));
		}

		if (isNewUser)
		{
			await _auditWriter.WriteAsync("platform", "platform.user.created", nameof(ApplicationUser), user.Id, $"Created platform user '{user.Email}'.", true, cancellationToken);
		}
		else
		{
			await _auditWriter.WriteAsync("platform", "platform.user.updated", nameof(ApplicationUser), user.Id, $"Updated platform user '{user.Email}'.", true, cancellationToken);
		}

		return user.Id;
	}

	public async Task<PlatformUserInviteResultModel> CreatePlatformUserInvitationAsync(PlatformUserInviteModel model, string baseUrl, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.UsersManage, cancellationToken);

		var actor = await RequirePlatformAccessAsync(cancellationToken);
		var desiredPermissionKeys = model.Permissions
			.Where(permission => permission.Selected)
			.Select(permission => permission.PermissionKey)
			.Distinct(StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
		if (model.IsPlatformSuperAdmin || desiredPermissionKeys.Count > 0)
		{
			await RequirePlatformPermissionAsync(PlatformPermissionKeys.PermissionsManage, cancellationToken);
		}

		if (model.TenantId.HasValue)
		{
			var tenantExists = await _dbContext.Tenants
				.AsNoTracking()
				.AnyAsync(tenant => tenant.Id == model.TenantId.Value, cancellationToken);
			if (!tenantExists)
			{
				throw new InvalidOperationException("The selected tenant was not found.");
			}
		}

		var normalizedEmail = model.Email.Trim();
		var pendingInvitations = await _dbContext.PlatformUserInvitations
			.Where(invitation => invitation.Email.ToUpper() == normalizedEmail.ToUpper() && invitation.AcceptedUtc == null && invitation.RevokedUtc == null)
			.ToListAsync(cancellationToken);
		foreach (var invitation in pendingInvitations)
		{
			invitation.Revoke();
			await _auditWriter.WriteAsync("platform", "platform.user-invitation.revoked", nameof(PlatformUserInvitation), invitation.Id.ToString(), $"Pending platform invitation for '{invitation.Email}' was revoked before issuing a replacement.", true, cancellationToken);
		}

		var rawToken = CreateInvitationToken();
		var permissionKeysJson = JsonSerializer.Serialize(model.IsPlatformSuperAdmin ? Array.Empty<string>() : desiredPermissionKeys.OrderBy(static key => key).ToArray(), JsonOptions);
		var entity = new PlatformUserInvitation(
			normalizedEmail,
			model.IsPlatformUserEnabled,
			model.IsPlatformSuperAdmin,
			permissionKeysJson,
			model.TenantId,
			model.TenantId.HasValue ? model.TenantRole : null,
			HashInvitationToken(rawToken),
			DateTimeOffset.UtcNow.AddDays(7),
			actor.UserId);
		_dbContext.PlatformUserInvitations.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);

		await _auditWriter.WriteAsync("platform", "platform.user-invited", nameof(PlatformUserInvitation), entity.Id.ToString(), $"Platform invitation created for '{entity.Email}'.", true, cancellationToken);
		if (entity.TenantId.HasValue)
		{
			await _dbContext.Entry(entity)
				.Reference(item => item.Tenant)
				.LoadAsync(cancellationToken);
		}

		await QueuePlatformInvitationEmailAsync(baseUrl, entity, rawToken, cancellationToken);

		return new PlatformUserInviteResultModel
		{
			Token = rawToken,
			Email = entity.Email,
			ExpiresAtUtc = entity.ExpiresAtUtc,
			InvitationUrl = $"/Account/AcceptPlatformInvite?token={Uri.EscapeDataString(rawToken)}"
		};
	}

	public async Task<PlatformUserInvitationAcceptanceContextModel> GetPlatformUserInvitationAcceptanceAsync(string token, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return new PlatformUserInvitationAcceptanceContextModel
			{
				Token = string.Empty,
				IsValid = false,
				ErrorMessage = "The invitation token is missing."
			};
		}

		var invitation = await FindUsablePlatformInvitationAsync(token, asTracking: false, cancellationToken);
		if (invitation is null)
		{
			return new PlatformUserInvitationAcceptanceContextModel
			{
				Token = token,
				IsValid = false,
				ErrorMessage = "The platform invitation was not found or has expired."
			};
		}

		var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
		var permissionKeys = ParsePlatformInvitationPermissions(invitation);
		var permissionLabels = invitation.IsPlatformSuperAdmin
			? ["Full platform administration"]
			: permissionKeys
				.Select(permissionKey => $"{PlatformPermissionCatalog.Get(permissionKey).Category}: {PlatformPermissionCatalog.Get(permissionKey).Label}")
				.ToArray();

		return new PlatformUserInvitationAcceptanceContextModel
		{
			Token = token,
			IsValid = true,
			Email = invitation.Email,
			IsPlatformUserEnabled = invitation.IsPlatformUserEnabled,
			IsPlatformSuperAdmin = invitation.IsPlatformSuperAdmin,
			ExistingAccountFound = existingUser is not null,
			TenantName = invitation.Tenant?.Name,
			TenantRole = invitation.TenantRole,
			ExpiresAtUtc = invitation.ExpiresAtUtc,
			PermissionLabels = permissionLabels
		};
	}

	public async Task<string> AcceptPlatformUserInvitationAsync(string token, string userId, CancellationToken cancellationToken = default)
	{
		var invitation = await FindUsablePlatformInvitationAsync(token, asTracking: true, cancellationToken)
			?? throw new InvalidOperationException("The platform invitation was not found or has expired.");
		var user = await _userManager.Users
			.Include(item => item.PlatformPermissions)
			.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
			?? throw new InvalidOperationException("The current user account was not found.");

		if (!string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
		{
			throw new UnauthorizedAccessException("The signed-in account does not match the invited email address.");
		}

		user.IsPlatformUserEnabled = invitation.IsPlatformUserEnabled;
		user.IsPlatformSuperAdmin = invitation.IsPlatformSuperAdmin;
		await ApplyPlatformUserPermissionsAsync(user, invitation.IsPlatformSuperAdmin ? [] : ParsePlatformInvitationPermissions(invitation), cancellationToken);

		int? createdMembershipId = null;
		if (invitation.TenantId.HasValue)
		{
			var existingMembership = await _dbContext.TenantMemberships
				.FirstOrDefaultAsync(membership => membership.TenantId == invitation.TenantId.Value && membership.UserId == user.Id, cancellationToken);
			if (existingMembership is null)
			{
				var membership = new TenantMembership(invitation.TenantId.Value, user.Id, invitation.TenantRole ?? TenantRole.User);
				_dbContext.TenantMemberships.Add(membership);
				await _dbContext.SaveChangesAsync(cancellationToken);
				createdMembershipId = membership.Id;
				await _auditWriter.WriteAsync("tenant", "tenant.user.invited-platform", nameof(TenantMembership), membership.Id.ToString(), $"Platform invitation added '{invitation.Email}' to tenant '{invitation.TenantId.Value}' as '{membership.Role}'.", true, cancellationToken);
			}
			else if (!user.ActiveTenantMembershipId.HasValue && existingMembership.IsEnabled)
			{
				createdMembershipId = existingMembership.Id;
			}
		}

		if (createdMembershipId.HasValue)
		{
			user.ActiveTenantMembershipId = createdMembershipId.Value;
		}

		var updateResult = await _userManager.UpdateAsync(user);
		if (!updateResult.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(static error => error.Description)));
		}

		invitation.Accept();
		await _dbContext.SaveChangesAsync(cancellationToken);
		await _auditWriter.WriteAsync("platform", "platform.user-invitation.accepted", nameof(PlatformUserInvitation), invitation.Id.ToString(), $"Platform invitation accepted for '{invitation.Email}'.", true, cancellationToken);

		return user.Id;
	}

	public async Task<PagedResult<PlatformThemeListItem>> GetPlatformThemesAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.SettingsManage, cancellationToken);

		var query = _dbContext.PlatformThemes
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(theme =>
				theme.Name.Contains(term) ||
				theme.Key.Contains(term) ||
				theme.Description.Contains(term));
		}

		var defaultThemeKey = await GetCurrentDefaultThemeKeyAsync(cancellationToken);
		var tenantUsageLookup = await _dbContext.TenantSettings
			.AsNoTracking()
			.GroupBy(setting => setting.ThemePresetKey)
			.Select(group => new
			{
				ThemePresetKey = group.Key,
				Count = group.Count()
			})
			.ToDictionaryAsync(item => item.ThemePresetKey, item => item.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);
		var themes = await query
			.OrderBy(theme => theme.Name)
			.ThenBy(theme => theme.Key)
			.ToListAsync(cancellationToken);
		var items = themes
			.Select(theme => new PlatformThemeListItem
			{
				Id = theme.Id,
				Key = theme.Key,
				Name = theme.Name,
				Description = theme.Description,
				PrimaryColor = theme.PrimaryColor,
				AccentColor = theme.AccentColor,
				BackgroundColor = theme.BackgroundColor,
				IsEnabled = theme.IsEnabled,
				IsArchived = theme.IsArchived,
				IsDefaultTheme = string.Equals(theme.Key, defaultThemeKey, StringComparison.OrdinalIgnoreCase),
				TenantUsageCount = tenantUsageLookup.GetValueOrDefault(theme.Key),
				UpdatedUtc = theme.UpdatedUtc
			})
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["name"] = [nameof(PlatformThemeListItem.Name)],
			["key"] = [nameof(PlatformThemeListItem.Key)],
			["description"] = [nameof(PlatformThemeListItem.Description)],
			["status"] = [nameof(PlatformThemeListItem.IsArchived), nameof(PlatformThemeListItem.IsEnabled)],
			["primary"] = [nameof(PlatformThemeListItem.PrimaryColor)],
			["accent"] = [nameof(PlatformThemeListItem.AccentColor)],
			["background"] = [nameof(PlatformThemeListItem.BackgroundColor)],
			["usage"] = [nameof(PlatformThemeListItem.TenantUsageCount)],
			["updated"] = [nameof(PlatformThemeListItem.UpdatedUtc)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(PlatformThemeListItem.Id)).ToList();
		var normalizedRequest = NormalizePagedQuery(request);
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, orderedItems.Count, normalizedRequest.Offset);
	}

	public async Task<PlatformThemeEditModel> GetPlatformThemeAsync(int? id, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.SettingsManage, cancellationToken);

		if (!id.HasValue)
		{
			var seed = SiteThemePresetCatalog.GetSeedModels()
				.First(seed => string.Equals(seed.Key, SiteThemePresetCatalog.DefaultPresetKey, StringComparison.OrdinalIgnoreCase));
			return new PlatformThemeEditModel
			{
				Key = seed.Key,
				Name = seed.Name,
				Description = seed.Description,
				PrimaryColor = seed.PrimaryColor,
				AccentColor = seed.AccentColor,
				BackgroundColor = seed.BackgroundColor,
				CssVariablesBlock = seed.CssVariablesBlock
			};
		}

		var theme = await _dbContext.PlatformThemes
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested theme was not found.");
		var defaultThemeKey = await GetCurrentDefaultThemeKeyAsync(cancellationToken);
		var tenantUsageCount = await _dbContext.TenantSettings
			.AsNoTracking()
			.CountAsync(setting => setting.ThemePresetKey == theme.Key, cancellationToken);
		var replacementThemeOptions = await BuildReplacementThemeOptionsAsync(theme.Id, defaultThemeKey, cancellationToken);

		return new PlatformThemeEditModel
		{
			Id = theme.Id,
			Key = theme.Key,
			Name = theme.Name,
			Description = theme.Description,
			PrimaryColor = theme.PrimaryColor,
			AccentColor = theme.AccentColor,
			BackgroundColor = theme.BackgroundColor,
			CssVariablesBlock = theme.CssVariablesBlock,
			IsEnabled = theme.IsEnabled,
			IsArchived = theme.IsArchived,
			IsDefaultTheme = string.Equals(theme.Key, defaultThemeKey, StringComparison.OrdinalIgnoreCase),
			TenantUsageCount = tenantUsageCount,
			ReplacementThemeOptions = replacementThemeOptions,
			UpdatedUtc = theme.UpdatedUtc
		};
	}

	public async Task<int> SavePlatformThemeAsync(PlatformThemeEditModel model, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.SettingsManage, cancellationToken);

		var normalizedKey = model.Key.Trim();
		var normalizedName = model.Name.Trim();
		var normalizedDescription = model.Description.Trim();
		var normalizedCssVariablesBlock = model.CssVariablesBlock.Trim();
		var duplicateExists = await _dbContext.PlatformThemes
			.AsNoTracking()
			.AnyAsync(theme => theme.Key == normalizedKey && (!model.Id.HasValue || theme.Id != model.Id.Value), cancellationToken);
		if (duplicateExists)
		{
			throw new InvalidOperationException("A global theme with this key already exists.");
		}

		PlatformTheme entity;
		var isNew = !model.Id.HasValue;
		if (isNew)
		{
			entity = new PlatformTheme(
				normalizedKey,
				normalizedName,
				normalizedDescription,
				model.PrimaryColor,
				model.AccentColor,
				model.BackgroundColor,
				normalizedCssVariablesBlock);
			_dbContext.PlatformThemes.Add(entity);
		}
		else
		{
			entity = await _dbContext.PlatformThemes
				.FirstOrDefaultAsync(theme => theme.Id == model.Id!.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested theme was not found.");

			if (!string.Equals(entity.Key, normalizedKey, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("The theme key cannot be changed after the theme is created.");
			}

			entity.UpdateIdentity(normalizedKey, normalizedName, normalizedDescription);
			entity.UpdatePresentation(model.PrimaryColor, model.AccentColor, model.BackgroundColor, normalizedCssVariablesBlock);
			if (model.IsArchived)
			{
				entity.Archive();
			}
			else if (model.IsEnabled)
			{
				entity.Enable();
			}
			else
			{
				entity.Disable();
			}
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		await _auditWriter.WriteAsync(
			"platform",
			isNew ? "platform.theme.created" : "platform.theme.updated",
			nameof(PlatformTheme),
			entity.Id.ToString(),
			$"Global theme '{entity.Name}' ({entity.Key}) was {(isNew ? "created" : "updated")}.",
			true,
			cancellationToken);

		return entity.Id;
	}

	public async Task SetPlatformThemeEnabledAsync(int id, bool isEnabled, int? replacementThemeId = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.SettingsManage, cancellationToken);

		var theme = await _dbContext.PlatformThemes
			.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested theme was not found.");

		PlatformTheme? replacementTheme = null;
		var replacedSiteDefault = false;
		var replacedTenantCount = 0;
		if (isEnabled)
		{
			theme.Enable();
		}
		else
		{
			(replacementTheme, replacedSiteDefault, replacedTenantCount) = await ReplaceThemeUsageAsync(theme, replacementThemeId, cancellationToken);
			theme.Disable();
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		var details = $"Global theme '{theme.Name}' ({theme.Key}) was {(isEnabled ? "enabled" : "disabled")}.";
		if (!isEnabled && replacementTheme is not null)
		{
			details = $"{details} Replaced with '{replacementTheme.Name}' for {(replacedSiteDefault ? "the site default and " : string.Empty)}{replacedTenantCount} tenant assignment(s).";
		}

		await _auditWriter.WriteAsync(
			"platform",
			isEnabled ? "platform.theme.enabled" : "platform.theme.disabled",
			nameof(PlatformTheme),
			theme.Id.ToString(),
			details,
			true,
			cancellationToken);
	}

	public async Task SetPlatformThemeArchivedAsync(int id, bool isArchived, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.SettingsManage, cancellationToken);

		var theme = await _dbContext.PlatformThemes
			.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested theme was not found.");

		if (isArchived)
		{
			theme.Archive();
		}
		else
		{
			theme.Enable();
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		await _auditWriter.WriteAsync(
			"platform",
			isArchived ? "platform.theme.archived" : "platform.theme.restored",
			nameof(PlatformTheme),
			theme.Id.ToString(),
			$"Global theme '{theme.Name}' ({theme.Key}) was {(isArchived ? "archived" : "restored")}.",
			true,
			cancellationToken);
	}

	public async Task DeletePlatformThemeAsync(int id, int? replacementThemeId = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.SettingsManage, cancellationToken);

		var theme = await _dbContext.PlatformThemes
			.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested theme was not found.");
		var (replacementTheme, replacedSiteDefault, replacedTenantCount) = await ReplaceThemeUsageAsync(theme, replacementThemeId, cancellationToken);

		_dbContext.PlatformThemes.Remove(theme);
		await _dbContext.SaveChangesAsync(cancellationToken);
		var details = $"Global theme '{theme.Name}' ({theme.Key}) was deleted.";
		if (replacementTheme is not null)
		{
			details = $"{details} Replaced with '{replacementTheme.Name}' for {(replacedSiteDefault ? "the site default and " : string.Empty)}{replacedTenantCount} tenant assignment(s).";
		}

		await _auditWriter.WriteAsync(
			"platform",
			"platform.theme.deleted",
			nameof(PlatformTheme),
			theme.Id.ToString(),
			details,
			true,
			cancellationToken);
	}

	private async Task<string?> GetCurrentDefaultThemeKeyAsync(CancellationToken cancellationToken)
	{
		return await _dbContext.SiteSettings
			.AsNoTracking()
			.Where(setting => setting.Id == SiteSetting.DefaultId)
			.Select(setting => setting.ThemePresetKey)
			.FirstOrDefaultAsync(cancellationToken);
	}

	private async Task<IReadOnlyList<SelectOption>> BuildReplacementThemeOptionsAsync(int excludedThemeId, string? defaultThemeKey, CancellationToken cancellationToken)
	{
		var themes = await _dbContext.PlatformThemes
			.AsNoTracking()
			.Where(theme => theme.Id != excludedThemeId && theme.IsEnabled && !theme.IsArchived)
			.Select(theme => new
			{
				theme.Id,
				theme.Key,
				theme.Name
			})
			.ToListAsync(cancellationToken);

		return themes
			.OrderBy(theme => string.Equals(theme.Key, defaultThemeKey, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
			.ThenBy(theme => theme.Name)
			.ThenBy(theme => theme.Key)
			.Select(theme => new SelectOption
			{
				Value = theme.Id.ToString(),
				Label = string.Equals(theme.Key, defaultThemeKey, StringComparison.OrdinalIgnoreCase)
					? $"{theme.Name} (Default)"
					: theme.Name
			})
			.ToArray();
	}

	private async Task<(PlatformTheme? ReplacementTheme, bool ReplacedSiteDefault, int ReplacedTenantCount)> ReplaceThemeUsageAsync(PlatformTheme theme, int? replacementThemeId, CancellationToken cancellationToken)
	{
		var siteSettings = await _dbContext.SiteSettings
			.Where(setting => setting.ThemePresetKey == theme.Key)
			.ToListAsync(cancellationToken);
		var tenantSettings = await _dbContext.TenantSettings
			.Where(setting => setting.ThemePresetKey == theme.Key)
			.ToListAsync(cancellationToken);
		if (siteSettings.Count == 0 && tenantSettings.Count == 0)
		{
			return (null, false, 0);
		}

		if (!replacementThemeId.HasValue)
		{
			throw new InvalidOperationException("Select a replacement theme before disabling or deleting a theme that is currently in use.");
		}

		var replacementTheme = await _dbContext.PlatformThemes
			.FirstOrDefaultAsync(item => item.Id == replacementThemeId.Value, cancellationToken)
			?? throw new InvalidOperationException("The replacement theme was not found.");
		if (replacementTheme.Id == theme.Id)
		{
			throw new InvalidOperationException("The replacement theme must be different from the theme being changed.");
		}

		if (!replacementTheme.IsEnabled || replacementTheme.IsArchived)
		{
			throw new InvalidOperationException("The replacement theme must be active before it can be assigned to the site or tenants.");
		}

		foreach (var setting in siteSettings)
		{
			setting.UpdateThemePreset(replacementTheme.Key);
		}

		foreach (var setting in tenantSettings)
		{
			setting.UpdateThemePreset(replacementTheme.Key);
		}

		return (replacementTheme, siteSettings.Count > 0, tenantSettings.Count);
	}

	public async Task<PagedResult<PlatformTenantListItem>> GetPlatformTenantsAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.TenantsView, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var query = _dbContext.Tenants
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(tenant =>
				tenant.Name.Contains(term) ||
				tenant.Slug.Contains(term));
		}

		var tenants = await query
			.OrderBy(tenant => tenant.Name)
			.ThenBy(tenant => tenant.Id)
			.Select(tenant => new PlatformTenantListItem
			{
				Id = tenant.Id,
				Name = tenant.Name,
				Slug = tenant.Slug,
				MembershipCount = tenant.Memberships.Count,
				EnabledMembershipCount = tenant.Memberships.Count(membership => membership.IsEnabled),
				OwnerCount = tenant.Memberships.Count(membership => membership.IsEnabled && membership.Role == TenantRole.Owner),
				PendingInvitationCount = 0,
				CreatedUtc = tenant.CreatedUtc,
				UpdatedUtc = tenant.UpdatedUtc
			})
			.ToListAsync(cancellationToken);
		var normalizedRequest = NormalizePagedQuery(request);
		if (tenants.Count == 0)
		{
			return CreatePagedResult<PlatformTenantListItem>([], 0, normalizedRequest.Offset);
		}

		var tenantIds = tenants.Select(item => item.Id).ToHashSet();
		var nowUtc = DateTimeOffset.UtcNow;
		var pendingInvitationCounts = (await _dbContext.TenantInvitations
			.AsNoTracking()
			.Where(invitation =>
				invitation.AcceptedUtc == null &&
				invitation.RevokedUtc == null)
			.Select(invitation => new
			{
				invitation.TenantId,
				invitation.ExpiresAtUtc
			})
			.ToListAsync(cancellationToken))
			.Where(invitation => tenantIds.Contains(invitation.TenantId) && invitation.ExpiresAtUtc > nowUtc)
			.GroupBy(invitation => invitation.TenantId)
			.ToDictionary(group => group.Key, group => group.Count());
		var ownerMemberships = await _dbContext.TenantMemberships
			.AsNoTracking()
			.Where(membership => tenantIds.Contains(membership.TenantId) && membership.Role == TenantRole.Owner)
			.Select(membership => new
			{
				membership.TenantId,
				membership.UserId
			})
			.ToListAsync(cancellationToken);
		var ownerUserIds = ownerMemberships
			.Select(membership => membership.UserId)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		var ownerLookup = await _userManager.Users
			.AsNoTracking()
			.Where(user => ownerUserIds.Contains(user.Id))
			.ToDictionaryAsync(user => user.Id, cancellationToken);
		var tenantOwnerLookup = ownerMemberships
			.GroupBy(membership => membership.TenantId)
			.ToDictionary(
				group => group.Key,
				group =>
				{
					var ownerMembership = group.First();
					return ownerLookup.TryGetValue(ownerMembership.UserId, out var user)
						? user
						: null;
				});

		foreach (var item in tenants)
		{
			item.PendingInvitationCount = pendingInvitationCounts.GetValueOrDefault(item.Id);
			if (tenantOwnerLookup.TryGetValue(item.Id, out var owner) && owner is not null)
			{
				item.OwnerDisplayName = BuildDisplayName(owner.FirstName, owner.LastName, owner.Email);
				item.OwnerEmail = owner.Email ?? string.Empty;
			}
		}

		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["tenant"] = [nameof(PlatformTenantListItem.Name), nameof(PlatformTenantListItem.Slug)],
			["members"] = [nameof(PlatformTenantListItem.MembershipCount)],
			["enabled"] = [nameof(PlatformTenantListItem.EnabledMembershipCount)],
			["owner"] = [nameof(PlatformTenantListItem.OwnerDisplayName)],
			["pendinginvites"] = [nameof(PlatformTenantListItem.PendingInvitationCount)],
			["updated"] = [nameof(PlatformTenantListItem.UpdatedUtc)]
		};
		var orderedItems = ApplyRequestedSorts(tenants.AsQueryable(), request, sortMap, nameof(PlatformTenantListItem.Id)).ToList();
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, orderedItems.Count, normalizedRequest.Offset);
	}

	public async Task<PlatformTenantDetailModel> GetPlatformTenantAsync(int tenantId, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.TenantsOversight, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var tenant = await _dbContext.Tenants
			.AsNoTracking()
			.Include(item => item.Memberships)
				.ThenInclude(membership => membership.PermissionOverrides)
			.Include(item => item.Invitations)
			.Include(item => item.Settings)
			.FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested tenant was not found.");

		var userIds = tenant.Memberships.Select(membership => membership.UserId).Distinct(StringComparer.Ordinal).ToArray();
		var users = await _userManager.Users
			.AsNoTracking()
			.Where(user => userIds.Contains(user.Id))
			.ToDictionaryAsync(user => user.Id, cancellationToken);

		return new PlatformTenantDetailModel
		{
			Id = tenant.Id,
			Name = tenant.Name,
			Slug = tenant.Slug,
			ThemePresetKey = tenant.Settings.FirstOrDefault()?.ThemePresetKey ?? SiteThemePresetCatalog.DefaultPresetKey,
			MembershipCount = tenant.Memberships.Count,
			EnabledMembershipCount = tenant.Memberships.Count(membership => membership.IsEnabled),
			OwnerCount = tenant.Memberships.Count(membership => membership.IsEnabled && membership.Role == TenantRole.Owner),
			OwnerDisplayName = tenant.Memberships
				.Where(membership => membership.IsEnabled && membership.Role == TenantRole.Owner)
				.Select(membership => users.TryGetValue(membership.UserId, out var user) ? BuildDisplayName(user.FirstName, user.LastName, user.Email) : string.Empty)
				.FirstOrDefault() ?? string.Empty,
			OwnerEmail = tenant.Memberships
				.Where(membership => membership.IsEnabled && membership.Role == TenantRole.Owner)
				.Select(membership => users.TryGetValue(membership.UserId, out var user) ? user.Email ?? string.Empty : string.Empty)
				.FirstOrDefault() ?? string.Empty,
			PendingInvitationCount = tenant.Invitations.Count(invitation => invitation.AcceptedUtc == null && invitation.RevokedUtc == null && invitation.ExpiresAtUtc > DateTimeOffset.UtcNow),
			PeopleCount = await _dbContext.People.CountAsync(person => person.TenantId == tenantId, cancellationToken),
			LocationCount = await _dbContext.Locations.CountAsync(location => location.TenantId == tenantId, cancellationToken),
			SurveyCount = await _dbContext.SurveyDefinitions.CountAsync(definition => definition.TenantId == tenantId, cancellationToken),
			AssignmentCount = await _dbContext.SurveyAssignments.CountAsync(assignment => assignment.TenantId == tenantId, cancellationToken),
			ResponseCount = await _dbContext.SurveyResponses.CountAsync(response => response.TenantId == tenantId, cancellationToken),
			GoalCount = await _dbContext.Goals.CountAsync(goal => goal.TenantId == tenantId, cancellationToken),
			AreaCount = await _dbContext.Areas.CountAsync(area => area.TenantId == tenantId, cancellationToken),
			CreatedUtc = tenant.CreatedUtc,
			UpdatedUtc = tenant.UpdatedUtc,
			Memberships = tenant.Memberships
				.OrderBy(membership => membership.Role)
				.ThenBy(membership => users.TryGetValue(membership.UserId, out var user) ? user.LastName : null)
				.ThenBy(membership => users.TryGetValue(membership.UserId, out var user) ? user.FirstName : null)
				.Select(membership =>
				{
					users.TryGetValue(membership.UserId, out var user);
					return new PlatformTenantMembershipListItem
					{
						MembershipId = membership.Id,
						UserId = membership.UserId,
						FullName = BuildDisplayName(user?.FirstName, user?.LastName, user?.Email),
						Email = user?.Email ?? string.Empty,
						Role = membership.Role,
						IsEnabled = membership.IsEnabled,
						PermissionOverrideCount = membership.PermissionOverrides.Count,
						EffectivePermissionCount = ResolveEffectiveTenantPermissions(membership).Count
					};
				})
				.ToList()
		};
	}

	public async Task<PlatformTenantEditModel> GetPlatformTenantEditAsync(int tenantId, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.TenantsManage, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var tenant = await _dbContext.Tenants
			.AsNoTracking()
			.Include(item => item.Settings)
			.FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested tenant was not found.");

		return new PlatformTenantEditModel
		{
			Id = tenant.Id,
			Name = tenant.Name,
			Slug = tenant.Slug,
			ThemePresetKey = tenant.Settings.FirstOrDefault()?.ThemePresetKey ?? SiteThemePresetCatalog.DefaultPresetKey,
			UpdatedUtc = tenant.UpdatedUtc
		};
	}

	public async Task SavePlatformTenantAsync(PlatformTenantEditModel model, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.TenantsManage, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var tenant = await _dbContext.Tenants
			.FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken)
			?? throw new InvalidOperationException("The requested tenant was not found.");

		var originalName = tenant.Name;
		tenant.Update(model.Name);

		var ownerUserId = await _dbContext.TenantMemberships
			.AsNoTracking()
			.Where(membership => membership.TenantId == tenant.Id && membership.Role == TenantRole.Owner)
			.Select(membership => membership.UserId)
			.FirstOrDefaultAsync(cancellationToken);
		if (!string.IsNullOrWhiteSpace(ownerUserId)
			&& await _dbContext.TenantMemberships
				.AsNoTracking()
				.AnyAsync(
					membership => membership.UserId == ownerUserId
						&& membership.Role == TenantRole.Owner
						&& membership.TenantId != tenant.Id
						&& membership.Tenant.Slug == tenant.Slug,
					cancellationToken))
		{
			throw new InvalidOperationException("That tenant owner already owns another tenant with this name. Please choose a different tenant name.");
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		if (!string.Equals(originalName, tenant.Name, StringComparison.Ordinal))
		{
			await _auditWriter.WriteAsync(
				"platform",
				"platform.tenant.updated",
				nameof(Tenant),
				tenant.Id.ToString(),
				$"Tenant renamed from '{originalName}' to '{tenant.Name}'.",
				true,
				cancellationToken);
		}
	}

	public async Task<PagedResult<AuditLogListItem>> GetAuditLogsAsync(
		PagedQuery request,
		string? plane = null,
		int? tenantId = null,
		bool? succeeded = null,
		string? search = null,
		CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.AuditView, cancellationToken);
		await RequirePlatformAccessAsync(cancellationToken);

		var query = _dbContext.AuditLogs
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(plane))
		{
			query = query.Where(log => log.Plane == plane);
		}

		if (tenantId.HasValue)
		{
			query = query.Where(log => log.TenantId == tenantId.Value);
		}

		if (succeeded.HasValue)
		{
			query = query.Where(log => log.Succeeded == succeeded.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(log =>
				log.ActionType.Contains(term) ||
				log.TargetType.Contains(term) ||
				(log.TargetId != null && log.TargetId.Contains(term)) ||
				(log.Details != null && log.Details.Contains(term)));
		}

		var normalizedRequest = NormalizePagedQuery(request);
		var logs = await query.ToListAsync(cancellationToken);
		var tenantIds = logs
			.Where(log => log.TenantId.HasValue)
			.Select(log => log.TenantId!.Value)
			.Distinct()
			.ToArray();
		var actorUserIds = logs
			.Where(log => !string.IsNullOrWhiteSpace(log.ActorUserId))
			.Select(log => log.ActorUserId!)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		var tenantLookup = await _dbContext.Tenants
			.AsNoTracking()
			.Where(tenant => tenantIds.Contains(tenant.Id))
			.ToDictionaryAsync(tenant => tenant.Id, tenant => tenant.Name, cancellationToken);
		var userLookup = await _userManager.Users
			.AsNoTracking()
			.Where(user => actorUserIds.Contains(user.Id))
			.ToDictionaryAsync(user => user.Id, user => BuildDisplayName(user.FirstName, user.LastName, user.Email), cancellationToken);

		var items = logs
			.OrderByDescending(log => log.CreatedUtc)
			.ThenByDescending(log => log.Id)
			.Select(log => new AuditLogListItem
			{
				Id = log.Id,
				TenantId = log.TenantId,
				TenantName = log.TenantId.HasValue ? tenantLookup.GetValueOrDefault(log.TenantId.Value) : null,
				ActorUserId = log.ActorUserId,
				ActorDisplayName = !string.IsNullOrWhiteSpace(log.ActorUserId)
					? userLookup.GetValueOrDefault(log.ActorUserId, log.ActorUserId)
					: "Anonymous / system",
				Plane = log.Plane,
				ActionType = log.ActionType,
				TargetType = log.TargetType,
				TargetId = log.TargetId,
				Details = log.Details,
				Succeeded = log.Succeeded,
				CreatedUtc = log.CreatedUtc
			})
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["when"] = [nameof(AuditLogListItem.CreatedUtc)],
			["plane"] = [nameof(AuditLogListItem.Plane)],
			["tenant"] = [nameof(AuditLogListItem.TenantName)],
			["actor"] = [nameof(AuditLogListItem.ActorDisplayName)],
			["action"] = [nameof(AuditLogListItem.ActionType)],
			["target"] = [nameof(AuditLogListItem.TargetType), nameof(AuditLogListItem.TargetId)],
			["result"] = [nameof(AuditLogListItem.Succeeded)],
			["details"] = [nameof(AuditLogListItem.Details)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(AuditLogListItem.Id), tieBreakerDescending: true).ToList();
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, orderedItems.Count, normalizedRequest.Offset);
	}

	private PlatformUserEditModel BuildPlatformUserEditModel(ApplicationUser? user, string currentUserId)
	{
		var selectedPermissions = user?.PlatformPermissions
			.Select(permission => permission.PermissionKey)
			.ToHashSet(StringComparer.Ordinal)
			?? new HashSet<string>(StringComparer.Ordinal);

		return new PlatformUserEditModel
		{
			Id = user?.Id,
			FirstName = user?.FirstName ?? string.Empty,
			LastName = user?.LastName ?? string.Empty,
			Email = user?.Email ?? string.Empty,
			IsPlatformUserEnabled = user?.IsPlatformUserEnabled ?? false,
			IsPlatformSuperAdmin = user?.IsPlatformSuperAdmin ?? false,
			IsBootstrapPlatformOwner = user?.IsBootstrapPlatformOwner ?? false,
			IsCurrentUser = user is not null && string.Equals(user.Id, currentUserId, StringComparison.Ordinal),
			Permissions = PlatformPermissionCatalog.All
				.Select(definition => new PlatformUserPermissionEditModel
				{
					PermissionKey = definition.Key,
					Category = definition.Category,
					PermissionLabel = definition.Label,
					Selected = user?.IsPlatformSuperAdmin == true || selectedPermissions.Contains(definition.Key)
				})
				.ToList()
		};
	}

	private async Task ApplyPlatformUserPermissionsAsync(ApplicationUser user, IReadOnlyCollection<string> desiredPermissionKeys, CancellationToken cancellationToken)
	{
		var existingPermissions = await _dbContext.PlatformUserPermissions
			.Where(permission => permission.UserId == user.Id)
			.ToListAsync(cancellationToken);
		var desiredLookup = desiredPermissionKeys.ToHashSet(StringComparer.Ordinal);

		foreach (var existingPermission in existingPermissions.Where(permission => !desiredLookup.Contains(permission.PermissionKey)).ToList())
		{
			_dbContext.PlatformUserPermissions.Remove(existingPermission);
		}

		var existingLookup = existingPermissions
			.Select(permission => permission.PermissionKey)
			.ToHashSet(StringComparer.Ordinal);
		foreach (var permissionKey in desiredLookup.Where(permissionKey => !existingLookup.Contains(permissionKey)))
		{
			_dbContext.PlatformUserPermissions.Add(new PlatformUserPermission(user.Id, permissionKey));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task EnsurePlatformUserGuardrailsAsync(
		string userId,
		bool desiredEnabled,
		bool desiredSuperAdmin,
		IReadOnlyCollection<string> desiredPermissionKeys,
		CancellationToken cancellationToken)
	{
		var users = await _userManager.Users
			.AsNoTracking()
			.Include(user => user.PlatformPermissions)
			.ToListAsync(cancellationToken);

		var enabledSuperAdminCount = 0;
		var enabledOperatorCount = 0;

		foreach (var existingUser in users)
		{
			var isCurrent = string.Equals(existingUser.Id, userId, StringComparison.Ordinal);
			var isEnabled = isCurrent ? desiredEnabled : existingUser.IsPlatformUserEnabled;
			var isSuperAdmin = isCurrent ? desiredSuperAdmin : existingUser.IsPlatformSuperAdmin;
			var permissionCount = isCurrent ? desiredPermissionKeys.Count : existingUser.PlatformPermissions.Count;

			if (isEnabled && isSuperAdmin)
			{
				enabledSuperAdminCount++;
			}

			if (isEnabled && (isSuperAdmin || permissionCount > 0))
			{
				enabledOperatorCount++;
			}
		}

		if (enabledSuperAdminCount <= 0)
		{
			throw new InvalidOperationException("The final enabled platform super admin cannot be removed or disabled.");
		}

		if (enabledOperatorCount <= 0)
		{
			throw new InvalidOperationException("The final enabled platform administrator cannot be removed or disabled.");
		}
	}

	private static bool MatchesPlatformUserSearch(ApplicationUser user, string search)
	{
		var term = search.Trim();
		if (string.IsNullOrWhiteSpace(term))
		{
			return true;
		}

		if ((user.FirstName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.LastName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.UserName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
		{
			return true;
		}

		if (user.IsPlatformSuperAdmin && "super admin".Contains(term, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (user.IsPlatformUserEnabled && "enabled".Contains(term, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!user.IsPlatformUserEnabled && "disabled".Contains(term, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return user.PlatformPermissions.Any(permission =>
			permission.PermissionKey.Contains(term, StringComparison.OrdinalIgnoreCase)
			|| PlatformPermissionCatalog.Get(permission.PermissionKey).Category.Contains(term, StringComparison.OrdinalIgnoreCase)
			|| PlatformPermissionCatalog.Get(permission.PermissionKey).Label.Contains(term, StringComparison.OrdinalIgnoreCase));
	}

	private async Task<PlatformUserInvitation?> FindUsablePlatformInvitationAsync(string token, bool asTracking, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return null;
		}

		var tokenHash = HashInvitationToken(token);
		var query = _dbContext.PlatformUserInvitations
			.Include(invitation => invitation.Tenant)
			.Where(invitation => invitation.TokenHash == tokenHash);
		if (!asTracking)
		{
			query = query.AsNoTracking();
		}

		var invitation = await query.FirstOrDefaultAsync(cancellationToken);
		if (invitation is null || !invitation.IsUsable(DateTimeOffset.UtcNow))
		{
			return null;
		}

		return invitation;
	}

	private static IReadOnlyCollection<string> ParsePlatformInvitationPermissions(PlatformUserInvitation invitation)
	{
		try
		{
			var values = JsonSerializer.Deserialize<string[]>(invitation.PermissionKeysJson, JsonOptions) ?? [];
			return values
				.Where(static value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.Ordinal)
				.ToArray();
		}
		catch
		{
			return [];
		}
	}
}
