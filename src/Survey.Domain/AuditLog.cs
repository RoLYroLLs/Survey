namespace Survey.Domain;

public class AuditLog
{
	public int Id { get; private set; }
	public int? TenantId { get; private set; }
	public string? ActorUserId { get; private set; }
	public string Plane { get; private set; } = string.Empty;
	public string ActionType { get; private set; } = string.Empty;
	public string TargetType { get; private set; } = string.Empty;
	public string? TargetId { get; private set; }
	public string? Details { get; private set; }
	public bool Succeeded { get; private set; }
	public DateTimeOffset CreatedUtc { get; private set; }

	private AuditLog()
	{
	}

	public AuditLog(int? tenantId, string? actorUserId, string plane, string actionType, string targetType, string? targetId, string? details, bool succeeded)
	{
		TenantId = tenantId > 0 ? tenantId : null;
		ActorUserId = CleanOptional(actorUserId, 450);
		Plane = RequireValue(plane, nameof(plane), 50);
		ActionType = RequireValue(actionType, nameof(actionType), 200);
		TargetType = RequireValue(targetType, nameof(targetType), 200);
		TargetId = CleanOptional(targetId, 200);
		Details = CleanOptional(details, 4000);
		Succeeded = succeeded;
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
