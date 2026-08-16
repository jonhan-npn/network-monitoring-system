namespace NetworkMonitor.Application.DTOs;

public class AlertRuleDto
{
    public Guid Id { get; init; }
    public Guid? DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Metric { get; init; } = string.Empty; // Availability | Cpu | Memory
    public double? ThresholdPercent { get; init; }
    public int ConsecutiveBreaches { get; init; }
    public string Channel { get; init; } = string.Empty; // Email | Telegram
    public string Destination { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public DateTime? LastTriggeredAt { get; init; }
}

public class AlertHistoryItemDto
{
    public long Id { get; init; }
    public Guid RuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public Guid? DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public DateTime TriggeredAt { get; init; }
}
