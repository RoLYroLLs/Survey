namespace Survey.Domain;

public class Goal : ITenantOwned
{
	public int Id { get; private set; }
	public int TenantId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string? Description { get; private set; }
	public int? AreaId { get; private set; }
	public int? SurveyDefinitionId { get; private set; }
	public int TargetResponseCount { get; private set; }
	public DateOnly StartDate { get; private set; }
	public DateOnly EndDate { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }
	public DateTimeOffset UpdatedUtc { get; private set; }
	public Area? Area { get; private set; }
	public SurveyDefinition? SurveyDefinition { get; private set; }

	private Goal()
	{
	}

	public Goal(
		string name,
		string? description,
		int? areaId,
		int? surveyDefinitionId,
		int targetResponseCount,
		DateOnly startDate,
		DateOnly endDate)
	{
		CreatedUtc = DateTimeOffset.UtcNow;
		Update(name, description, areaId, surveyDefinitionId, targetResponseCount, startDate, endDate);
	}

	public void Update(
		string name,
		string? description,
		int? areaId,
		int? surveyDefinitionId,
		int targetResponseCount,
		DateOnly startDate,
		DateOnly endDate)
	{
		if (targetResponseCount < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(targetResponseCount));
		}

		if (endDate < startDate)
		{
			throw new ArgumentOutOfRangeException(nameof(endDate), "The end date must be on or after the start date.");
		}

		Name = RequireValue(name, nameof(name), 200);
		Description = CleanOptional(description, 2000);
		AreaId = areaId > 0 ? areaId : null;
		SurveyDefinitionId = surveyDefinitionId > 0 ? surveyDefinitionId : null;
		TargetResponseCount = targetResponseCount;
		StartDate = startDate;
		EndDate = endDate;
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
