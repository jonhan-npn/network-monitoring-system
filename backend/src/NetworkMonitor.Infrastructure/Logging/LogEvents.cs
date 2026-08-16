using Microsoft.Extensions.Logging;

namespace NetworkMonitor.Infrastructure.Logging;

/// <summary>
/// Stable EventIds for the Worker's background services and Infrastructure's
/// notifiers. Not required, but makes it possible to filter/alert on
/// specific log categories in whatever aggregator you point container logs
/// at (Loki, CloudWatch, etc.) without parsing message text.
/// </summary>
public static class LogEvents
{
    public static readonly EventId PingSweepStarted = new(1000, nameof(PingSweepStarted));
    public static readonly EventId PingSweepCompleted = new(1001, nameof(PingSweepCompleted));
    public static readonly EventId DeviceStatusChanged = new(1002, nameof(DeviceStatusChanged));

    public static readonly EventId SnmpPollStarted = new(1100, nameof(SnmpPollStarted));
    public static readonly EventId SnmpPollCompleted = new(1101, nameof(SnmpPollCompleted));

    public static readonly EventId SshInventoryStarted = new(1200, nameof(SshInventoryStarted));
    public static readonly EventId SshInventoryCompleted = new(1201, nameof(SshInventoryCompleted));

    public static readonly EventId AlertEvaluated = new(1300, nameof(AlertEvaluated));
    public static readonly EventId AlertDispatched = new(1301, nameof(AlertDispatched));
    public static readonly EventId AlertDispatchFailed = new(1302, nameof(AlertDispatchFailed));

    public static readonly EventId RetentionPurgeCompleted = new(1400, nameof(RetentionPurgeCompleted));
}
