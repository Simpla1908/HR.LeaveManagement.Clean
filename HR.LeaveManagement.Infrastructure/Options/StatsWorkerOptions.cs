namespace HR.LeaveManagement.Infrastructure.Options;

public sealed class StatsWorkerOptions
{
    public const string SectionName = "StatsWorker";
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(3);
}
