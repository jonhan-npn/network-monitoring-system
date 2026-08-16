namespace NetworkMonitor.Worker;

/// <summary>
/// Bound from the "Monitoring" appsettings section. Falls back to
/// NetworkMonitor.Shared.Constants.PollingDefaults for anything not
/// explicitly configured — see Program.cs.
/// </summary>
public class MonitoringWorkerOptions
{
    public const string SectionName = "Monitoring";

    public int PingIntervalSeconds { get; set; } = Shared.Constants.PollingDefaults.PingIntervalSeconds;
    public int PingTimeoutMs { get; set; } = Shared.Constants.PollingDefaults.PingTimeoutMs;

    public int SnmpIntervalSeconds { get; set; } = Shared.Constants.PollingDefaults.SnmpIntervalSeconds;

    public int SshInventoryIntervalMinutes { get; set; } = Shared.Constants.PollingDefaults.SshInventoryIntervalMinutes;

    /// <summary>How long ping_results/resource_samples rows are kept before DataRetentionJob purges them — see docs/database.md.</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>How often DataRetentionJob runs its purge sweep.</summary>
    public int RetentionCheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Minimum time between repeat dispatches of the SAME alert rule while
    /// its breach condition persists (e.g. a device that's been offline for
    /// hours doesn't get a fresh email every 30 seconds) — see
    /// AlertThresholdEngine.
    /// </summary>
    public int AlertReNotifyCooldownMinutes { get; set; } = 15;
}
