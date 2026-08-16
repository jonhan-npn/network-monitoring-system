using System.Collections.Concurrent;

namespace NetworkMonitor.Worker.Monitoring;

/// <summary>
/// Tracks "how many times in a row has this device been offline / over a
/// resource threshold" purely in memory. Deliberately NOT persisted —
/// counters reset to 0 on Worker restart, which just means a rule needing
/// e.g. 3 consecutive breaches waits for 3 fresh samples after a restart
/// before it can fire again. That's a reasonable trade for staying out of
/// the database (no extra table, no extra writes every 30 seconds just to
/// track a streak) — see AlertThresholdEngine for how these counts are used.
///
/// Keyed by (DeviceId, MetricKey) so availability and CPU/memory streaks
/// for the same device don't collide.
/// </summary>
public class ConsecutiveCounterTracker
{
    private readonly ConcurrentDictionary<(Guid DeviceId, string MetricKey), int> _counters = new();

    /// <summary>Increments and returns the new streak length.</summary>
    public int Increment(Guid deviceId, string metricKey) =>
        _counters.AddOrUpdate((deviceId, metricKey), 1, (_, current) => current + 1);

    /// <summary>Resets the streak to 0 (call this when the condition is no longer breached).</summary>
    public void Reset(Guid deviceId, string metricKey) =>
        _counters[(deviceId, metricKey)] = 0;

    public int Current(Guid deviceId, string metricKey) =>
        _counters.GetValueOrDefault((deviceId, metricKey), 0);
}
