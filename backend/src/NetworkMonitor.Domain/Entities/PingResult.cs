namespace NetworkMonitor.Domain.Entities;

/// <summary>
/// One ping sweep result for one device. Maps to the `ping_results` table.
/// Uses a `long` identity rather than a GUID — this table takes one insert
/// per device per 30-second sweep, and nothing outside the backend ever
/// references a row by id, so a GUID surrogate key would only add index
/// bloat (see docs/database.md).
/// </summary>
public class PingResult
{
    public long Id { get; set; }

    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }

    public bool IsOnline { get; set; }
    public double? LatencyMs { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
