namespace Survey.Domain;

public class SeedState
{
	public SeedState(string key, int version)
	{
		Key = key;
		Version = version;
		AppliedUtc = DateTimeOffset.UtcNow;
	}

	private SeedState()
	{
		Key = string.Empty;
	}

	public string Key { get; private set; }

	public int Version { get; private set; }

	public DateTimeOffset AppliedUtc { get; private set; }

	public void MarkApplied(int version)
	{
		Version = version;
		AppliedUtc = DateTimeOffset.UtcNow;
	}
}
