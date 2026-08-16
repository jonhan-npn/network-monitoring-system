namespace NetworkMonitor.Application.Interfaces;

public record SnmpPollResult(bool Success, double? CpuPercent, double? MemoryPercent, string? ErrorMessage, DateTime SampledAt);

public record SnmpTarget(
    Guid DeviceId,
    string IpAddress,
    string Community,
    int Port,
    string Version); // "1" | "2c" | "3"

/// <summary>
/// Abstraction over SNMP GET requests for CPU/RAM (standard hosts use
/// HOST-RESOURCES-MIB / UCD-MIB OIDs; vendor devices may need
/// vendor-specific OIDs — the implementation resolves the right OID set
/// per device, this interface just asks for "give me CPU/RAM"). Implemented
/// by NetworkMonitor.Infrastructure.SNMP.SnmpPollingService using SnmpSharpNet
/// or a similar managed SNMP library.
/// </summary>
public interface ISnmpService
{
    Task<SnmpPollResult> PollResourceUsageAsync(SnmpTarget target, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Guid DeviceId, SnmpPollResult Result)>> PollManyAsync(
        IReadOnlyList<SnmpTarget> targets,
        CancellationToken cancellationToken = default);
}
