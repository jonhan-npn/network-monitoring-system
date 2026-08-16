namespace NetworkMonitor.Shared.Constants;

/// <summary>
/// Fallback intervals/timeouts for the Worker's background services, used
/// when a value isn't overridden in appsettings' MonitoringOptions.
/// </summary>
public static class PollingDefaults
{
    public const int PingIntervalSeconds = 30;
    public const int PingTimeoutMs = 2000;
    public const int SnmpIntervalSeconds = 60;
    public const int SshInventoryIntervalMinutes = 30;
}
