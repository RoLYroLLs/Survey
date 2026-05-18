namespace Survey.Domain;

public class Tenant
{
	public int Id { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string Slug { get; private set; } = string.Empty;
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public ICollection<TenantMembership> Memberships { get; } = new List<TenantMembership>();
	public ICollection<TenantInvitation> Invitations { get; } = new List<TenantInvitation>();
	public ICollection<TenantSetting> Settings { get; } = new List<TenantSetting>();
	public ICollection<TenantVisibleCountry> VisibleCountries { get; } = new List<TenantVisibleCountry>();
	public ICollection<TenantVisibleStateProvince> VisibleStateProvinces { get; } = new List<TenantVisibleStateProvince>();
	public ICollection<TenantVisibleCounty> VisibleCounties { get; } = new List<TenantVisibleCounty>();

	private Tenant()
	{
	}

	public Tenant(string name)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(name);
	}

	public void Update(string name)
	{
		Name = RequireValue(name, nameof(name), 200);
		Slug = NormalizeSlug(Name);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	private static string NormalizeSlug(string value)
	{
		var slug = new string(
			value
				.Trim()
				.ToLowerInvariant()
				.Select(character => char.IsLetterOrDigit(character) ? character : '-')
				.ToArray());

		while (slug.Contains("--", StringComparison.Ordinal))
		{
			slug = slug.Replace("--", "-", StringComparison.Ordinal);
		}

		return slug.Trim('-');
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
