using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Infrastructure.Ping;

/// <summary>
/// IPingService via System.Net.NetworkInformation.Ping — the built-in BCL
/// wrapper around ICMP echo, no external package needed. On Linux (the
/// target deployment per docs/deployment.md), raw ICMP sockets normally
/// require elevated privileges; the .NET runtime handles this via
/// SOCK_DGRAM ICMP on modern Linux kernels without needing root, but if you
/// see PlatformNotSupportedException or permission errors in a locked-down
/// container, grant the container NET_RAW capability
/// (`cap_add: [NET_RAW]` in docker-compose) rather than running as root.
/// </summary>
public class PingSweepService : IPingService
{
    // Caps how many concurrent ICMP probes run at once. Unbounded
    // parallelism across, say, 200 devices would open 200 sockets
    // simultaneously — fine on a beefy host, less fine on a small VM. 20
    // concurrent probes sweeps 200 devices in ~10 batches, comfortably
    // inside the 30-second polling interval even with a 2s timeout per host.
    private const int MaxConcurrency = 20;

    private readonly ILogger<PingSweepService> _logger;

    public PingSweepService(ILogger<PingSweepService> logger) => _logger = logger;

    public async Task<PingCheckResult> PingOnceAsync(string ipAddress, int timeoutMs = 2000, CancellationToken cancellationToken = default)
    {
        using var ping = new System.Net.NetworkInformation.Ping();
        var checkedAt = DateTime.UtcNow;

        try
        {
            var reply = await ping.SendPingAsync(ipAddress, timeoutMs);

            return reply.Status == IPStatus.Success
                ? new PingCheckResult(true, reply.RoundtripTime, checkedAt)
                : new PingCheckResult(false, null, checkedAt);
        }
        catch (PingException ex)
        {
            // Typically DNS resolution failure or an invalid address — log
            // at Debug, not Warning/Error, since this fires routinely for a
            // device that's simply unreachable, which is an expected,
            // not exceptional, outcome for this service.
            _logger.LogDebug(ex, "Ping to {IpAddress} failed", ipAddress);
            return new PingCheckResult(false, null, checkedAt);
        }
    }

    public async Task<IReadOnlyList<(Guid DeviceId, PingCheckResult Result)>> PingManyAsync(
        IReadOnlyList<(Guid DeviceId, string IpAddress)> devices,
        int timeoutMs = 2000,
        CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(MaxConcurrency);

        var tasks = devices.Select(async device =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await PingOnceAsync(device.IpAddress, timeoutMs, cancellationToken);
                return (device.DeviceId, Result: result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }
}
