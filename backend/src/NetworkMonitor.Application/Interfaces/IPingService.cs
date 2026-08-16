namespace NetworkMonitor.Application.Interfaces;

public record PingCheckResult(bool IsOnline, double? LatencyMs, DateTime CheckedAt);

/// <summary>
/// Abstraction over ICMP echo requests so the Worker's scheduling logic
/// never touches System.Net.NetworkInformation directly. Implemented by
/// NetworkMonitor.Infrastructure.Ping.PingSweepService.
/// </summary>
public interface IPingService
{
    /// <summary>Pings a single host once, with the given timeout.</summary>
    Task<PingCheckResult> PingOnceAsync(string ipAddress, int timeoutMs = 2000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pings many hosts concurrently (bounded — see implementation for the
    /// max degree of parallelism) and returns one result per host, in the
    /// order the devices were given.
    /// </summary>
    Task<IReadOnlyList<(Guid DeviceId, PingCheckResult Result)>> PingManyAsync(
        IReadOnlyList<(Guid DeviceId, string IpAddress)> devices,
        int timeoutMs = 2000,
        CancellationToken cancellationToken = default);
}
