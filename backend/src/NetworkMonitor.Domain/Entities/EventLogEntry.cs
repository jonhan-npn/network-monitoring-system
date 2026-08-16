using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Domain.Entities;

/// <summary>
/// An audited state-change event (online/offline transition, threshold
/// breach, alert sent, device added/removed/updated). Maps to the
/// `event_log_entries` table. `DeviceId` is nullable with ON DELETE SET NULL
/// so the log entry survives even after the device itself is deleted.
/// </summary>
public class EventLogEntry
{
    public long Id { get; set; }

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public EventType EventType { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Raw JSON string (maps to the `jsonb` column). Kept as a string here
    /// rather than a strongly-typed payload per event type, since different
    /// EventTypes carry different shapes (e.g. AlertSent carries
    /// { ruleId, ruleName, channel }; a Cpu/Memory breach carries
    /// { previousValue, newValue }). Callers parse defensively — see
    /// AlertRuleQueries.ParseAlertMetadata for the AlertSent case.
    /// </summary>
    public string? Metadata { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
