namespace NetworkMonitor.API.Configuration;

/// <summary>
/// Strongly typed binding for the "Monitoring" section in appsettings.json.
/// Shared by the API (for read endpoints) and the Worker (which actually polls).
/// </summary>
public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>How often devices are pinged, in seconds.</summary>
    public int PingIntervalSeconds { get; set; } = 30;

    /// <summary>How often SNMP CPU/RAM polling runs, in seconds.</summary>
    public int SnmpPollIntervalSeconds { get; set; } = 60;

    /// <summary>Ping timeout in milliseconds before a device is considered unreachable.</summary>
    public int PingTimeoutMs { get; set; } = 2000;

    /// <summary>Number of consecutive failed pings before a device is marked offline.</summary>
    public int OfflineThreshold { get; set; } = 2;

    /// <summary>Number of days of ping/resource history to retain before pruning.</summary>
    public int HistoryRetentionDays { get; set; } = 30;
}
