namespace Survey.Domain;

public class SurveyDefinition
{
	public int Id { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string? Description { get; private set; }
	public bool IsArchived { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public ICollection<SurveyVersion> Versions { get; } = new List<SurveyVersion>();
	public ICollection<Goal> Goals { get; } = new List<Goal>();

	private SurveyDefinition()
	{
	}

	public SurveyDefinition(string name, string? description)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(name, description);
	}

	public void Update(string name, string? description)
	{
		Name = RequireValue(name, nameof(name), 200);
		Description = CleanOptional(description, 2000);
		UpdatedUtc = DateTimeOffset.UtcNow;
	}

	public void SetArchived(bool isArchived)
	{
		if (IsArchived == isArchived)
		{
			return;
		}

		IsArchived = isArchived;
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

	private static string? CleanOptional(string? value, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var trimmed = value.Trim();
		return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
	}
}
