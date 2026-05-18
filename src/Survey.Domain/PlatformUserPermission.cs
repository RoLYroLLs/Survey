namespace Survey.Domain;

public class PlatformUserPermission
{
	public int Id { get; private set; }
	public string UserId { get; private set; } = string.Empty;
	public string PermissionKey { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }

	private PlatformUserPermission()
	{
	}

	public PlatformUserPermission(string userId, string permissionKey)
	{
		UserId = RequireValue(userId, nameof(userId), 450);
		PermissionKey = RequireValue(permissionKey, nameof(permissionKey), 200);
		CreatedUtc = DateTimeOffset.UtcNow;
	}

	private static string RequireValue(string? value, string paramName, int maxLength)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			throw new ArgumentException("A value is required.", paramName);
		}

		return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
	}
}
