namespace Survey.Infrastructure.Configuration;

public sealed class BackgroundJobsOptions
{
	public const string SectionName = "BackgroundJobs";

	public bool Enabled { get; set; } = true;
	public string DashboardPath { get; set; } = "/admin/hangfire";
	public int WorkerCount { get; set; } = 1;
	public int PollingIntervalMilliseconds { get; set; } = 750;
	public string DefaultQueueName { get; set; } = "default";
	public string SetupQueueName { get; set; } = "setup";
	public string EmailQueueName { get; set; } = "email";
}
