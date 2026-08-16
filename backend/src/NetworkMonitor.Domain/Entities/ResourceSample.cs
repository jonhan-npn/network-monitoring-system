namespace NetworkMonitor.Domain.Entities;

/// <summary>
/// One SNMP CPU/RAM poll result for one device. Maps to the
/// `resource_samples` table. Same long-identity rationale as PingResult.
/// </summary>
public class ResourceSample
{
    public long Id { get; set; }

    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }

    public double? CpuPercent { get; set; }
    public double? MemoryPercent { get; set; }
    public DateTime SampledAt { get; set; } = DateTime.UtcNow;
}
