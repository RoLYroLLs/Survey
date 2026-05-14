namespace Survey.Infrastructure.Configuration;

public class DatabaseOptions
{
	public const string SectionName = "Database";
	public const string Sqlite = "Sqlite";
	public const string SqlServer = "SqlServer";

	public string Provider { get; set; } = Sqlite;
}
