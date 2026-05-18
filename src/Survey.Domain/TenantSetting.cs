namespace Survey.Domain;

public class TenantSetting : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public string ThemePresetKey { get; private set; } = string.Empty;
	public DateTimeOffset UpdatedUtc { get; private set; }
	public Tenant Tenant { get; private set; } = default!;

	private TenantSetting()
	{
	}

	public TenantSetting(int tenantId, string themePresetKey)
	{
		if (tenantId < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(tenantId));
		}

		TenantId = tenantId;
		UpdateThemePreset(themePresetKey);
	}

	public void UpdateThemePreset(string themePresetKey)
	{
		ThemePresetKey = RequireValue(themePresetKey, nameof(themePresetKey), 100);
		UpdatedUtc = DateTimeOffset.UtcNow;
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
