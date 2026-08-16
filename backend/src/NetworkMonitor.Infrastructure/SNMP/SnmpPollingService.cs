using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Infrastructure.SNMP;

/// <summary>
/// ISnmpService via Lextm.SharpSnmpLib. Polls the UCD-SNMP-MIB OIDs that
/// net-snmp (the standard Linux SNMP agent, "snmpd") exposes — these are
/// NOT universal. Cisco/vendor switches and routers expose CPU/RAM under
/// different, vendor-specific OIDs (e.g. Cisco's CISCO-PROCESS-MIB
/// cpmCPUTotal5minRev, or CISCO-MEMORY-POOL-MIB). This implementation
/// covers the "Server" device type well out of the box; extending
/// ResolveOids per DeviceType (or per a vendor field on Device, if you add
/// one) is the natural next step for switch/router support — see the
/// TODO below rather than silently returning zeros for unsupported gear.
/// </summary>
public class SnmpPollingService : ISnmpService
{
    // UCD-SNMP-MIB (net-snmp) well-known OIDs.
    private static readonly ObjectIdentifier CpuIdleOid = new("1.3.6.1.4.1.2021.11.11.0");   // ssCpuIdle
    private static readonly ObjectIdentifier MemTotalOid = new("1.3.6.1.4.1.2021.4.5.0");    // memTotalReal (kB)
    private static readonly ObjectIdentifier MemAvailOid = new("1.3.6.1.4.1.2021.4.6.0");    // memAvailReal (kB)

    private const int MaxConcurrency = 10;
    private const int TimeoutMs = 3000;
    private const int Retries = 1;

    private readonly ILogger<SnmpPollingService> _logger;

    public SnmpPollingService(ILogger<SnmpPollingService> logger) => _logger = logger;

    public async Task<SnmpPollResult> PollResourceUsageAsync(SnmpTarget target, CancellationToken cancellationToken = default)
    {
        var sampledAt = DateTime.UtcNow;

        if (target.Version == "3")
        {
            // SNMPv3 needs auth/privacy protocol + credentials per device,
            // which the current Device schema doesn't carry (only
            // SnmpCommunity/Port/Version — see docs/database.md). Fail
            // loudly rather than silently attempting a v2c-shaped request
            // that will just time out.
            return new SnmpPollResult(false, null, null, "SNMPv3 is not yet supported by this Device schema (no auth/priv credential fields).", sampledAt);
        }

        try
        {
            var version = target.Version == "1" ? VersionCode.V1 : VersionCode.V2;
            var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(target.IpAddress), target.Port);
            var community = new OctetString(target.Community);

            var variables = new List<Variable>
            {
                new(CpuIdleOid),
                new(MemTotalOid),
                new(MemAvailOid)
            };

            var result = await Task.Run(
                () => Messenger.Get(version, endpoint, community, variables, TimeoutMs),
                cancellationToken);

            double? cpuPercent = null;
            double? memoryPercent = null;

            foreach (var variable in result)
            {
                if (variable.Id.Equals(CpuIdleOid) && TryGetInteger(variable.Data, out var idle))
                {
                    cpuPercent = Math.Clamp(100 - idle, 0, 100);
                }
            }

            var totalVar = result.FirstOrDefault(v => v.Id.Equals(MemTotalOid));
            var availVar = result.FirstOrDefault(v => v.Id.Equals(MemAvailOid));

            if (totalVar is not null && availVar is not null &&
                TryGetInteger(totalVar.Data, out var totalKb) &&
                TryGetInteger(availVar.Data, out var availKb) &&
                totalKb > 0)
            {
                memoryPercent = Math.Clamp((totalKb - availKb) / (double)totalKb * 100, 0, 100);
            }

            return new SnmpPollResult(true, cpuPercent, memoryPercent, null, sampledAt);
        }
        catch (Exception ex) when (ex is SnmpException or Lextm.SharpSnmpLib.Messaging.TimeoutException or System.Net.Sockets.SocketException)
        {
            _logger.LogDebug(ex, "SNMP poll failed for device {DeviceId} ({IpAddress})", target.DeviceId, target.IpAddress);
            return new SnmpPollResult(false, null, null, ex.Message, sampledAt);
        }
    }

    public async Task<IReadOnlyList<(Guid DeviceId, SnmpPollResult Result)>> PollManyAsync(
        IReadOnlyList<SnmpTarget> targets, CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(MaxConcurrency);

        var tasks = targets.Select(async target =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await PollResourceUsageAsync(target, cancellationToken);
                return (target.DeviceId, Result: result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    private static bool TryGetInteger(ISnmpData data, out int value)
    {
        switch (data)
        {
            case Integer32 i:
                value = i.ToInt32();
                return true;
            case Counter32 c:
                value = (int)c.ToUInt32();
                return true;
            case Gauge32 g:
                value = (int)g.ToUInt32();
                return true;
            default:
                value = 0;
                return false;
        }
    }
}
