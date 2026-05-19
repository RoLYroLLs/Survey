using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Identity;

public class SurveyUserStore(
	SurveyDbContext context,
	IdentityErrorDescriber describer) : UserStore<ApplicationUser, IdentityRole, SurveyDbContext, string>(context, describer), IUserPasskeyStore<ApplicationUser>
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private DbSet<ApplicationUserPasskey> Passkeys => Context.Set<ApplicationUserPasskey>();

	public async Task AddOrUpdatePasskeyAsync(ApplicationUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(user);
		ArgumentNullException.ThrowIfNull(passkey);
		cancellationToken.ThrowIfCancellationRequested();

		var existing = await Passkeys
			.SingleOrDefaultAsync(item => item.UserId == user.Id && item.CredentialId == passkey.CredentialId, cancellationToken);

		var payload = PasskeyPayload.FromInfo(passkey);
		var data = JsonSerializer.Serialize(payload, JsonOptions);

		if (existing is null)
		{
			Passkeys.Add(new ApplicationUserPasskey
			{
				CredentialId = passkey.CredentialId.ToArray(),
				UserId = user.Id,
				Data = data
			});
		}
		else
		{
			existing.Data = data;
		}

		await Context.SaveChangesAsync(cancellationToken);
	}

	public async Task<IList<UserPasskeyInfo>> GetPasskeysAsync(ApplicationUser user, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(user);
		cancellationToken.ThrowIfCancellationRequested();

		var records = await Passkeys
			.AsNoTracking()
			.Where(item => item.UserId == user.Id)
			.ToListAsync(cancellationToken);

		return records
			.Select(static item => item.ToUserPasskeyInfo())
			.Where(static item => item is not null)
			.Cast<UserPasskeyInfo>()
			.OrderByDescending(static item => item.CreatedAt)
			.ToList();
	}

	public async Task<ApplicationUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(credentialId);
		cancellationToken.ThrowIfCancellationRequested();

		var record = await Passkeys
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		var matched = record.FirstOrDefault(item => item.CredentialId.AsSpan().SequenceEqual(credentialId));
		if (matched is null)
		{
			return null;
		}

		return await Users.SingleOrDefaultAsync(user => user.Id == matched.UserId, cancellationToken);
	}

	public async Task<UserPasskeyInfo?> FindPasskeyAsync(ApplicationUser user, byte[] credentialId, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(user);
		ArgumentNullException.ThrowIfNull(credentialId);
		cancellationToken.ThrowIfCancellationRequested();

		var record = await Passkeys
			.AsNoTracking()
			.Where(item => item.UserId == user.Id)
			.ToListAsync(cancellationToken);

		return record
			.FirstOrDefault(item => item.CredentialId.AsSpan().SequenceEqual(credentialId))
			.ToUserPasskeyInfo();
	}

	public async Task RemovePasskeyAsync(ApplicationUser user, byte[] credentialId, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(user);
		ArgumentNullException.ThrowIfNull(credentialId);
		cancellationToken.ThrowIfCancellationRequested();

		var record = await Passkeys
			.Where(item => item.UserId == user.Id)
			.ToListAsync(cancellationToken);

		var matched = record.FirstOrDefault(item => item.CredentialId.AsSpan().SequenceEqual(credentialId));
		if (matched is null)
		{
			return;
		}

		Passkeys.Remove(matched);
		await Context.SaveChangesAsync(cancellationToken);
	}

	internal sealed class PasskeyPayload
	{
		public byte[] CredentialId { get; set; } = [];

		public byte[] PublicKey { get; set; } = [];

		public string? Name { get; set; }

		public DateTimeOffset CreatedAt { get; set; }

		public uint SignCount { get; set; }

		public string[] Transports { get; set; } = [];

		public bool IsUserVerified { get; set; }

		public bool IsBackupEligible { get; set; }

		public bool IsBackedUp { get; set; }

		public byte[]? AttestationObject { get; set; }

		public byte[]? ClientDataJson { get; set; }

		public static PasskeyPayload FromInfo(UserPasskeyInfo info)
		{
			return new PasskeyPayload
			{
				CredentialId = info.CredentialId.ToArray(),
				PublicKey = info.PublicKey.ToArray(),
				Name = info.Name,
				CreatedAt = info.CreatedAt,
				SignCount = info.SignCount,
				Transports = info.Transports?.ToArray() ?? [],
				IsUserVerified = info.IsUserVerified,
				IsBackupEligible = info.IsBackupEligible,
				IsBackedUp = info.IsBackedUp,
				AttestationObject = info.AttestationObject?.ToArray() ?? [],
				ClientDataJson = info.ClientDataJson?.ToArray() ?? []
			};
		}

		public UserPasskeyInfo ToInfo()
		{
			return new UserPasskeyInfo(
				CredentialId,
				PublicKey,
				CreatedAt,
				SignCount,
				Transports,
				IsUserVerified,
				IsBackupEligible,
				IsBackedUp,
				AttestationObject ?? [],
				ClientDataJson ?? [])
			{
				Name = Name
			};
		}
	}
}

internal static class ApplicationUserPasskeyExtensions
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public static UserPasskeyInfo? ToUserPasskeyInfo(this ApplicationUserPasskey? record)
	{
		if (record is null || string.IsNullOrWhiteSpace(record.Data))
		{
			return null;
		}

		var payload = JsonSerializer.Deserialize<SurveyUserStore.PasskeyPayload>(record.Data, JsonOptions);
		return payload?.ToInfo();
	}
}
