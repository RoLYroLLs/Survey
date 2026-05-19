using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<PagedResult<TenantUserListItem>> GetTenantUsersAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.UsersView, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		var memberships = await _dbContext.TenantMemberships
			.AsNoTracking()
			.Include(membership => membership.PermissionOverrides)
			.Where(membership => membership.TenantId == context.TenantId)
			.OrderBy(membership => membership.Id)
			.ToListAsync(cancellationToken);

		var userIds = memberships.Select(membership => membership.UserId).Distinct(StringComparer.Ordinal).ToArray();
		var users = await _userManager.Users
			.AsNoTracking()
			.Where(user => userIds.Contains(user.Id))
			.ToDictionaryAsync(user => user.Id, cancellationToken);

		var items = memberships
			.Select(membership =>
			{
				users.TryGetValue(membership.UserId, out var user);
				var effectivePermissions = ResolveEffectiveTenantPermissions(membership);

				return new TenantUserListItem
				{
					MembershipId = membership.Id,
					UserId = membership.UserId,
					FullName = BuildDisplayName(user?.FirstName, user?.LastName, user?.Email),
					Email = user?.Email ?? string.Empty,
					Role = membership.Role,
					IsEnabled = membership.IsEnabled,
					IsCurrentUser = string.Equals(membership.UserId, context.UserId, StringComparison.Ordinal),
					PermissionOverrideCount = membership.PermissionOverrides.Count,
					EffectivePermissionCount = effectivePermissions.Count
				};
			})
			.ToList();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			items = items
				.Where(item =>
					item.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
					|| item.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
					|| item.Role.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		items = items
			.OrderBy(item => item.FullName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(item => item.Email, StringComparer.OrdinalIgnoreCase)
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["name"] = [nameof(TenantUserListItem.FullName)],
			["email"] = [nameof(TenantUserListItem.Email)],
			["role"] = [nameof(TenantUserListItem.Role)],
			["status"] = [nameof(TenantUserListItem.IsEnabled)],
			["permissions"] = [nameof(TenantUserListItem.EffectivePermissionCount), nameof(TenantUserListItem.PermissionOverrideCount)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(TenantUserListItem.MembershipId)).ToList();
		var normalizedRequest = NormalizePagedQuery(request);
		var totalCount = orderedItems.Count;
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, totalCount, normalizedRequest.Offset);
	}

	public async Task<TenantUserEditModel> GetTenantUserAsync(int membershipId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.UsersView, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		var membership = await _dbContext.TenantMemberships
			.AsNoTracking()
			.Include(item => item.PermissionOverrides)
			.FirstOrDefaultAsync(item => item.Id == membershipId && item.TenantId == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested tenant user was not found.");
		var user = await _userManager.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(item => item.Id == membership.UserId, cancellationToken)
			?? throw new InvalidOperationException("The requested user account was not found.");

		var canChangeRole = context.TenantPermissions.Contains(TenantPermissionKeys.UsersChangeRole, StringComparer.Ordinal);
		var canManagePermissions = context.TenantPermissions.Contains(TenantPermissionKeys.UsersManagePermissions, StringComparer.Ordinal);
		var canEnableDisable = context.TenantPermissions.Contains(TenantPermissionKeys.UsersEnableDisable, StringComparer.Ordinal);
		var canRemove = context.TenantPermissions.Contains(TenantPermissionKeys.UsersRemove, StringComparer.Ordinal);
		var canReviewEffectivePermissions = canManagePermissions
			|| context.TenantPermissions.Contains(TenantPermissionKeys.UsersReviewEffectivePermissions, StringComparer.Ordinal);
		var overrideLookup = membership.PermissionOverrides.ToDictionary(permission => permission.PermissionKey, StringComparer.Ordinal);

		return new TenantUserEditModel
		{
			MembershipId = membership.Id,
			UserId = membership.UserId,
			FullName = BuildDisplayName(user.FirstName, user.LastName, user.Email),
			Email = user.Email ?? string.Empty,
			Role = membership.Role,
			IsEnabled = membership.IsEnabled,
			IsCurrentUser = string.Equals(membership.UserId, context.UserId, StringComparison.Ordinal),
			CanChangeRole = canChangeRole && membership.Role != TenantRole.Owner,
			CanManagePermissions = canManagePermissions,
			CanEnableDisable = canEnableDisable && membership.Role != TenantRole.Owner,
			CanRemove = canRemove && membership.Role != TenantRole.Owner,
			CanReviewEffectivePermissions = canReviewEffectivePermissions,
			Permissions = canReviewEffectivePermissions
				? TenantPermissionCatalog.All
					.Select(definition => new TenantUserPermissionOverrideEditModel
					{
						PermissionKey = definition.Key,
						Category = definition.Category,
						PermissionLabel = definition.Label,
						DefaultGranted = PermissionDefaults.GetTenantPermissions(membership.Role).Contains(definition.Key, StringComparer.Ordinal),
						OverrideMode = overrideLookup.TryGetValue(definition.Key, out var permission)
							? permission.GrantKind == PermissionGrantKind.Allow
								? TenantPermissionOverrideModes.Allow
								: TenantPermissionOverrideModes.Deny
							: TenantPermissionOverrideModes.Default
					})
					.ToList()
				: Array.Empty<TenantUserPermissionOverrideEditModel>()
		};
	}

	public async Task<int> SaveTenantUserAsync(TenantUserEditModel model, CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantAccessAsync(cancellationToken);
		var membership = await _dbContext.TenantMemberships
			.Include(item => item.PermissionOverrides)
			.FirstOrDefaultAsync(item => item.Id == model.MembershipId && item.TenantId == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested tenant user was not found.");

		var roleChanged = membership.Role != model.Role;
		var enabledChanged = membership.IsEnabled != model.IsEnabled;
		var permissionsChanged = PermissionOverridesChanged(membership.PermissionOverrides, model.Permissions);

		if (string.Equals(membership.UserId, context.UserId, StringComparison.Ordinal)
			&& (roleChanged || enabledChanged || permissionsChanged))
		{
			throw new InvalidOperationException("You cannot change your own tenant role, status, or permissions.");
		}

		if (roleChanged)
		{
			await RequireTenantPermissionAsync(TenantPermissionKeys.UsersChangeRole, cancellationToken);
			if (membership.Role == TenantRole.Owner)
			{
				throw new InvalidOperationException("The tenant owner role cannot be changed.");
			}
			EnsureRoleManagementAllowed(context, membership.Role, model.Role);
			await EnsureSingleOwnerConstraintAsync(membership.TenantId, membership.Id, model.Role, cancellationToken);
		}

		if (enabledChanged)
		{
			await RequireTenantPermissionAsync(TenantPermissionKeys.UsersEnableDisable, cancellationToken);
		}

		if (permissionsChanged)
		{
			await RequireTenantPermissionAsync(TenantPermissionKeys.UsersManagePermissions, cancellationToken);
		}

		await EnsureMembershipGuardrailsAsync(membership, model.Role, model.IsEnabled, removing: false, cancellationToken);

		if (roleChanged)
		{
			membership.ChangeRole(model.Role);
		}

		if (enabledChanged)
		{
			membership.SetEnabled(model.IsEnabled);
		}

		if (permissionsChanged)
		{
			ApplyPermissionOverrides(membership, model.Permissions);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		if (roleChanged)
		{
			await _auditWriter.WriteAsync("tenant", "tenant.user.role-changed", nameof(TenantMembership), membership.Id.ToString(), $"Role changed to '{model.Role}'.", true, cancellationToken);
		}

		if (enabledChanged)
		{
			await _auditWriter.WriteAsync("tenant", model.IsEnabled ? "tenant.user.enabled" : "tenant.user.disabled", nameof(TenantMembership), membership.Id.ToString(), $"Membership enabled state changed to '{model.IsEnabled}'.", true, cancellationToken);
		}

		if (permissionsChanged)
		{
			await _auditWriter.WriteAsync("tenant", "tenant.user.permissions-changed", nameof(TenantMembership), membership.Id.ToString(), "Permission overrides were updated.", true, cancellationToken);
		}

		return membership.Id;
	}

	public async Task RemoveTenantUserAsync(int membershipId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.UsersRemove, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		var membership = await _dbContext.TenantMemberships
			.Include(item => item.PermissionOverrides)
			.FirstOrDefaultAsync(item => item.Id == membershipId && item.TenantId == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested tenant user was not found.");

		if (string.Equals(membership.UserId, context.UserId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("You cannot remove your own tenant membership.");
		}

		EnsureRoleManagementAllowed(context, membership.Role, membership.Role);
		await EnsureMembershipGuardrailsAsync(membership, membership.Role, membership.IsEnabled, removing: true, cancellationToken);

		var user = await _userManager.FindByIdAsync(membership.UserId);
		_dbContext.TenantMemberships.Remove(membership);
		await _dbContext.SaveChangesAsync(cancellationToken);

		if (user is not null && user.ActiveTenantMembershipId == membershipId)
		{
			user.ActiveTenantMembershipId = null;
			await _userManager.UpdateAsync(user);
		}

		await _auditWriter.WriteAsync("tenant", "tenant.user.removed", nameof(TenantMembership), membershipId.ToString(), "The tenant membership was removed.", true, cancellationToken);
	}

	public async Task<TenantInvitationCreateResultModel> CreateTenantInvitationAsync(TenantUserInviteModel model, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.UsersInvite, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		if (model.Role == TenantRole.Owner)
		{
			await RequireTenantPermissionAsync(TenantPermissionKeys.UsersChangeRole, cancellationToken);
			if (context.TenantRole != TenantRole.Owner)
			{
				throw new UnauthorizedAccessException("Only a tenant owner can invite another owner.");
			}

			await EnsureSingleOwnerConstraintAsync(context.TenantId ?? 0, null, model.Role, cancellationToken);
		}

		var normalizedEmail = model.Email.Trim().ToUpperInvariant();
		var existingMembership = await (
			from membership in _dbContext.TenantMemberships
			join user in _userManager.Users on membership.UserId equals user.Id
			where membership.TenantId == context.TenantId
				&& user.NormalizedEmail == normalizedEmail
			select membership.Id)
			.FirstOrDefaultAsync(cancellationToken);
		if (existingMembership > 0)
		{
			throw new InvalidOperationException("That email address already belongs to a user in this tenant.");
		}

		var pendingInvitations = await _dbContext.TenantInvitations
			.Where(invitation => invitation.TenantId == context.TenantId && invitation.Email.ToUpper() == normalizedEmail && invitation.AcceptedUtc == null && invitation.RevokedUtc == null)
			.ToListAsync(cancellationToken);
		foreach (var invitation in pendingInvitations)
		{
			invitation.Revoke();
			await _auditWriter.WriteAsync("tenant", "tenant.invitation.revoked", nameof(TenantInvitation), invitation.Id.ToString(), $"Pending invitation for '{invitation.Email}' was revoked before issuing a replacement.", true, cancellationToken);
		}

		return await CreateTenantInvitationCoreAsync(context, model.Email.Trim(), model.Role, cancellationToken);
	}

	public async Task<PagedResult<TenantInvitationListItem>> GetTenantInvitationsAsync(PagedQuery request, bool includeHistory = true, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.UsersView, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		var invitations = await _dbContext.TenantInvitations
			.AsNoTracking()
			.Where(invitation => invitation.TenantId == context.TenantId)
			.ToListAsync(cancellationToken);
		invitations = invitations
			.OrderByDescending(invitation => invitation.CreatedUtc)
			.ToList();

		if (!includeHistory)
		{
			invitations = invitations
				.Where(invitation => invitation.AcceptedUtc == null && invitation.RevokedUtc == null && invitation.ExpiresAtUtc > DateTimeOffset.UtcNow)
				.ToList();
		}

		var createdByUserIds = invitations
			.Select(invitation => invitation.CreatedByUserId)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		var createdByLookup = await _userManager.Users
			.AsNoTracking()
			.Where(user => createdByUserIds.Contains(user.Id))
			.ToDictionaryAsync(user => user.Id, user => BuildDisplayName(user.FirstName, user.LastName, user.Email), cancellationToken);

		var nowUtc = DateTimeOffset.UtcNow;
		var items = invitations
			.Select(invitation => new TenantInvitationListItem
			{
				Id = invitation.Id,
				Email = invitation.Email,
				Role = invitation.Role,
				CreatedByDisplayName = createdByLookup.GetValueOrDefault(invitation.CreatedByUserId, invitation.CreatedByUserId),
				CreatedUtc = invitation.CreatedUtc,
				ExpiresAtUtc = invitation.ExpiresAtUtc,
				AcceptedUtc = invitation.AcceptedUtc,
				RevokedUtc = invitation.RevokedUtc,
				IsPending = invitation.AcceptedUtc == null && invitation.RevokedUtc == null && invitation.ExpiresAtUtc > nowUtc,
				IsExpired = invitation.AcceptedUtc == null && invitation.RevokedUtc == null && invitation.ExpiresAtUtc <= nowUtc,
				StatusSortOrder = invitation.AcceptedUtc != null
					? 1
					: invitation.RevokedUtc != null
						? 3
						: invitation.ExpiresAtUtc <= nowUtc
							? 2
							: 0
			})
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["email"] = [nameof(TenantInvitationListItem.Email)],
			["role"] = [nameof(TenantInvitationListItem.Role)],
			["status"] = [nameof(TenantInvitationListItem.StatusSortOrder)],
			["createdby"] = [nameof(TenantInvitationListItem.CreatedByDisplayName)],
			["created"] = [nameof(TenantInvitationListItem.CreatedUtc)],
			["expires"] = [nameof(TenantInvitationListItem.ExpiresAtUtc)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(TenantInvitationListItem.Id)).ToList();
		var normalizedRequest = NormalizePagedQuery(request);
		var totalCount = orderedItems.Count;
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, totalCount, normalizedRequest.Offset);
	}

	public async Task<TenantInvitationCreateResultModel> ReissueTenantInvitationAsync(int invitationId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.UsersInvite, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		var invitation = await _dbContext.TenantInvitations
			.FirstOrDefaultAsync(item => item.Id == invitationId && item.TenantId == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested invitation was not found.");
		if (invitation.AcceptedUtc is not null)
		{
			throw new InvalidOperationException("Accepted invitations cannot be reissued.");
		}

		if (invitation.Role == TenantRole.Owner && context.TenantRole != TenantRole.Owner)
		{
			throw new UnauthorizedAccessException("Only a tenant owner can invite another owner.");
		}

		if (invitation.Role == TenantRole.Owner)
		{
			await EnsureSingleOwnerConstraintAsync(context.TenantId ?? 0, null, invitation.Role, cancellationToken);
		}

		return await CreateTenantInvitationCoreAsync(context, invitation.Email, invitation.Role, cancellationToken);
	}

	public async Task RevokeTenantInvitationAsync(int invitationId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.UsersInvite, cancellationToken);

		var context = await RequireTenantAccessAsync(cancellationToken);
		var invitation = await _dbContext.TenantInvitations
			.FirstOrDefaultAsync(item => item.Id == invitationId && item.TenantId == context.TenantId, cancellationToken)
			?? throw new InvalidOperationException("The requested invitation was not found.");
		if (invitation.AcceptedUtc is not null)
		{
			throw new InvalidOperationException("Accepted invitations cannot be revoked.");
		}

		if (invitation.RevokedUtc is not null)
		{
			return;
		}

		invitation.Revoke();
		await _dbContext.SaveChangesAsync(cancellationToken);
		await _auditWriter.WriteAsync("tenant", "tenant.invitation.revoked", nameof(TenantInvitation), invitation.Id.ToString(), $"Invitation for '{invitation.Email}' was revoked.", true, cancellationToken);
	}

	public async Task<TenantInvitationAcceptanceContextModel> GetTenantInvitationAcceptanceAsync(string token, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return new TenantInvitationAcceptanceContextModel
			{
				Token = string.Empty,
				IsValid = false,
				ErrorMessage = "The invitation token is missing."
			};
		}

		var invitation = await FindUsableInvitationAsync(token, asTracking: false, cancellationToken);
		if (invitation is null)
		{
			return new TenantInvitationAcceptanceContextModel
			{
				Token = token,
				IsValid = false,
				ErrorMessage = "The invitation was not found or has expired."
			};
		}

		var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
		return new TenantInvitationAcceptanceContextModel
		{
			Token = token,
			IsValid = true,
			TenantName = invitation.Tenant.Name,
			Email = invitation.Email,
			Role = invitation.Role,
			ExistingAccountFound = existingUser is not null,
			ExpiresAtUtc = invitation.ExpiresAtUtc
		};
	}

	public async Task<string> AcceptTenantInvitationForExistingUserAsync(string token, string userId, CancellationToken cancellationToken = default)
	{
		var invitation = await FindUsableInvitationAsync(token, asTracking: true, cancellationToken)
			?? throw new InvalidOperationException("The invitation was not found or has expired.");
		var user = await _userManager.FindByIdAsync(userId)
			?? throw new InvalidOperationException("The current user account was not found.");

		if (!string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
		{
			throw new UnauthorizedAccessException("The signed-in account does not match the invited email address.");
		}

		await AcceptInvitationAsync(invitation, user, cancellationToken);
		return user.Id;
	}

	public async Task<string> AcceptTenantInvitationForNewUserAsync(TenantInvitationRegistrationModel model, CancellationToken cancellationToken = default)
	{
		var invitation = await FindUsableInvitationAsync(model.Token, asTracking: true, cancellationToken)
			?? throw new InvalidOperationException("The invitation was not found or has expired.");

		var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
		if (existingUser is not null)
		{
			throw new InvalidOperationException("An account already exists for this email address. Sign in first to accept the invitation.");
		}

		var user = new ApplicationUser
		{
			UserName = invitation.Email,
			Email = invitation.Email,
			EmailConfirmed = true,
			FirstName = model.FirstName.Trim(),
			LastName = model.LastName.Trim(),
			IsPlatformSuperAdmin = false,
			IsPlatformUserEnabled = false
		};
		UserAvatarPalette.EnsureAssigned(user);

		var createResult = await _userManager.CreateAsync(user, model.Password);
		if (!createResult.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(static error => error.Description)));
		}

		await AcceptInvitationAsync(invitation, user, cancellationToken);
		return user.Id;
	}

	private async Task<TenantInvitation?> FindUsableInvitationAsync(string token, bool asTracking, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return null;
		}

		var tokenHash = HashInvitationToken(token);
		var query = _dbContext.TenantInvitations
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

	private async Task<TenantInvitationCreateResultModel> CreateTenantInvitationCoreAsync(
		CurrentAccessContext context,
		string email,
		TenantRole role,
		CancellationToken cancellationToken)
	{
		var rawToken = CreateInvitationToken();
		var invitationEntity = new TenantInvitation(
			context.TenantId ?? throw new InvalidOperationException("An active tenant is required."),
			email,
			role,
			HashInvitationToken(rawToken),
			DateTimeOffset.UtcNow.AddDays(7),
			context.UserId);

		_dbContext.TenantInvitations.Add(invitationEntity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await _auditWriter.WriteAsync("tenant", "tenant.user.invited", nameof(TenantInvitation), invitationEntity.Id.ToString(), $"Invitation created for '{invitationEntity.Email}' with role '{invitationEntity.Role}'.", true, cancellationToken);

		return new TenantInvitationCreateResultModel
		{
			Token = rawToken,
			Email = invitationEntity.Email,
			Role = invitationEntity.Role,
			ExpiresAtUtc = invitationEntity.ExpiresAtUtc
		};
	}

	private async Task AcceptInvitationAsync(TenantInvitation invitation, ApplicationUser user, CancellationToken cancellationToken)
	{
		var existingMembership = await _dbContext.TenantMemberships
			.AsNoTracking()
			.FirstOrDefaultAsync(membership => membership.TenantId == invitation.TenantId && membership.UserId == user.Id, cancellationToken);
		if (existingMembership is not null)
		{
			throw new InvalidOperationException("This account is already a member of the tenant.");
		}

		await EnsureSingleOwnerConstraintAsync(invitation.TenantId, null, invitation.Role, cancellationToken);

		var membership = new TenantMembership(invitation.TenantId, user.Id, invitation.Role);
		_dbContext.TenantMemberships.Add(membership);
		await _dbContext.SaveChangesAsync(cancellationToken);

		user.ActiveTenantMembershipId = membership.Id;
		var updateResult = await _userManager.UpdateAsync(user);
		if (!updateResult.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(static error => error.Description)));
		}

		invitation.Accept();
		await _dbContext.SaveChangesAsync(cancellationToken);
		_tenantExecutionContext.UseTenant(invitation.TenantId);
		await _auditWriter.WriteAsync("tenant", "tenant.invitation.accepted", nameof(TenantInvitation), invitation.Id.ToString(), $"Invitation accepted for '{invitation.Email}'.", true, cancellationToken);
	}

	private async Task EnsureMembershipGuardrailsAsync(TenantMembership membership, TenantRole desiredRole, bool desiredEnabled, bool removing, CancellationToken cancellationToken)
	{
		var memberships = await _dbContext.TenantMemberships
			.AsNoTracking()
			.Where(item => item.TenantId == membership.TenantId)
			.ToListAsync(cancellationToken);

		var ownerCount = 0;
		var adminCapableCount = 0;
		foreach (var existingMembership in memberships)
		{
			if (existingMembership.Id == membership.Id)
			{
				if (removing)
				{
					continue;
				}

				if (desiredEnabled && desiredRole == TenantRole.Owner)
				{
					ownerCount++;
				}

				if (desiredEnabled && IsAdminCapable(desiredRole))
				{
					adminCapableCount++;
				}

				continue;
			}

			if (existingMembership.IsEnabled && existingMembership.Role == TenantRole.Owner)
			{
				ownerCount++;
			}

			if (existingMembership.IsEnabled && IsAdminCapable(existingMembership.Role))
			{
				adminCapableCount++;
			}
		}

		if (ownerCount <= 0)
		{
			throw new InvalidOperationException("The final tenant owner cannot be removed, disabled, or demoted.");
		}

		if (adminCapableCount <= 0)
		{
			throw new InvalidOperationException("The final owner or admin cannot be removed, disabled, or demoted.");
		}
	}

	private async Task EnsureSingleOwnerConstraintAsync(int tenantId, int? membershipId, TenantRole desiredRole, CancellationToken cancellationToken)
	{
		if (desiredRole != TenantRole.Owner)
		{
			return;
		}

		var ownerExists = await _dbContext.TenantMemberships
			.AsNoTracking()
			.AnyAsync(item =>
				item.TenantId == tenantId
				&& item.Role == TenantRole.Owner
				&& (!membershipId.HasValue || item.Id != membershipId.Value),
				cancellationToken);
		if (ownerExists)
		{
			throw new InvalidOperationException("A tenant can only have one owner.");
		}
	}

	private static IReadOnlySet<string> ResolveEffectiveTenantPermissions(TenantMembership membership)
	{
		var permissions = PermissionDefaults.GetTenantPermissions(membership.Role).ToHashSet(StringComparer.Ordinal);
		foreach (var overridePermission in membership.PermissionOverrides)
		{
			if (overridePermission.GrantKind == PermissionGrantKind.Allow)
			{
				permissions.Add(overridePermission.PermissionKey);
			}
			else
			{
				permissions.Remove(overridePermission.PermissionKey);
			}
		}

		return permissions;
	}

	private static bool PermissionOverridesChanged(
		IEnumerable<TenantMembershipPermission> existingPermissions,
		IReadOnlyList<TenantUserPermissionOverrideEditModel> desiredPermissions)
	{
		var existingLookup = existingPermissions.ToDictionary(
			permission => permission.PermissionKey,
			permission => permission.GrantKind == PermissionGrantKind.Allow ? TenantPermissionOverrideModes.Allow : TenantPermissionOverrideModes.Deny,
			StringComparer.Ordinal);
		var desiredLookup = desiredPermissions.ToDictionary(
			permission => permission.PermissionKey,
			permission => permission.OverrideMode,
			StringComparer.Ordinal);

		foreach (var definition in TenantPermissionCatalog.All)
		{
			var existingMode = existingLookup.GetValueOrDefault(definition.Key, TenantPermissionOverrideModes.Default);
			var desiredMode = desiredLookup.GetValueOrDefault(definition.Key, TenantPermissionOverrideModes.Default);
			if (!string.Equals(existingMode, desiredMode, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static void ApplyPermissionOverrides(
		TenantMembership membership,
		IReadOnlyList<TenantUserPermissionOverrideEditModel> desiredPermissions)
	{
		var desiredLookup = desiredPermissions.ToDictionary(permission => permission.PermissionKey, StringComparer.Ordinal);
		var existingPermissions = membership.PermissionOverrides.ToList();

		foreach (var existingPermission in existingPermissions)
		{
			var desiredMode = desiredLookup.GetValueOrDefault(existingPermission.PermissionKey)?.OverrideMode ?? TenantPermissionOverrideModes.Default;
			if (string.Equals(desiredMode, TenantPermissionOverrideModes.Default, StringComparison.Ordinal))
			{
				membership.PermissionOverrides.Remove(existingPermission);
				continue;
			}

			var desiredGrantKind = string.Equals(desiredMode, TenantPermissionOverrideModes.Allow, StringComparison.Ordinal)
				? PermissionGrantKind.Allow
				: PermissionGrantKind.Deny;
			if (existingPermission.GrantKind != desiredGrantKind)
			{
				membership.PermissionOverrides.Remove(existingPermission);
				membership.PermissionOverrides.Add(new TenantMembershipPermission(membership.Id, existingPermission.PermissionKey, desiredGrantKind));
			}
		}

		foreach (var desiredPermission in desiredPermissions)
		{
			if (string.Equals(desiredPermission.OverrideMode, TenantPermissionOverrideModes.Default, StringComparison.Ordinal))
			{
				continue;
			}

			if (membership.PermissionOverrides.Any(permission => string.Equals(permission.PermissionKey, desiredPermission.PermissionKey, StringComparison.Ordinal)))
			{
				continue;
			}

			membership.PermissionOverrides.Add(new TenantMembershipPermission(
				membership.Id,
				desiredPermission.PermissionKey,
				string.Equals(desiredPermission.OverrideMode, TenantPermissionOverrideModes.Allow, StringComparison.Ordinal)
					? PermissionGrantKind.Allow
					: PermissionGrantKind.Deny));
		}
	}

	private static void EnsureRoleManagementAllowed(CurrentAccessContext actor, TenantRole currentRole, TenantRole desiredRole)
	{
		if (currentRole == TenantRole.Owner && actor.TenantRole != TenantRole.Owner)
		{
			throw new UnauthorizedAccessException("Only a tenant owner can manage another owner.");
		}

		if (currentRole == TenantRole.Owner && desiredRole != TenantRole.Owner)
		{
			throw new InvalidOperationException("The tenant owner role cannot be changed.");
		}

		if (desiredRole == TenantRole.Owner && actor.TenantRole != TenantRole.Owner)
		{
			throw new UnauthorizedAccessException("Only a tenant owner can assign the owner role.");
		}
	}

	private static bool IsAdminCapable(TenantRole role)
	{
		return role is TenantRole.Owner or TenantRole.Admin;
	}

	private static string BuildDisplayName(string? firstName, string? lastName, string? email)
	{
		var fullName = string.Join(" ", new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
		return string.IsNullOrWhiteSpace(fullName) ? (email ?? string.Empty) : fullName;
	}

	private static string CreateInvitationToken()
	{
		return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
	}

	private static string HashInvitationToken(string token)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
	}
}
