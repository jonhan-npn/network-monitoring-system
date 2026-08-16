namespace NetworkMonitor.Application.DTOs;

public class DeviceStatusDto
{
    public Guid DeviceId { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public bool IsOnline { get; init; }
    public double? LatencyMs { get; init; }
    public DateTime CheckedAt { get; init; }
}

public class PingHistoryPointDto
{
    public bool IsOnline { get; init; }
    public double? LatencyMs { get; init; }
    public DateTime CheckedAt { get; init; }
}

public class ResourceHistoryPointDto
{
    public double? CpuPercent { get; init; }
    public double? MemoryPercent { get; init; }
    public DateTime SampledAt { get; init; }
}

public class DashboardSummaryDto
{
    public int TotalDevices { get; init; }
    public int OnlineDevices { get; init; }
    public int OfflineDevices { get; init; }
    public double AverageCpuPercent { get; init; }
    public double AverageMemoryPercent { get; init; }
    public int ActiveAlertRules { get; init; }
    public int AlertsTriggeredLast24h { get; init; }
}

public class EventLogDto
{
    public long Id { get; init; }
    public Guid? DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}
