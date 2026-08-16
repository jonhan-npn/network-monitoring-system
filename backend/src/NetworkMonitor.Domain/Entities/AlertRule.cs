using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Domain.Entities;

/// <summary>
/// Threshold configuration consumed by the Worker's AlertThresholdEngine.
/// Maps to the `alert_rules` table. `DeviceId == null` means the rule
/// applies to every monitored device. `Metric` is a plain string
/// ("Availability" | "Cpu" | "Memory") for the same reason `User.Role` is —
/// see docs/database.md.
/// </summary>
public class AlertRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public double? ThresholdPercent { get; set; }
    public int ConsecutiveBreaches { get; set; } = 1;

    public AlertChannel Channel { get; set; } = AlertChannel.Email;
    public string Destination { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
}
