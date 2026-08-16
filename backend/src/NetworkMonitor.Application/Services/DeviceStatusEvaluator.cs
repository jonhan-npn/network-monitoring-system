using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Services;

public record StatusEvaluation(bool StatusChanged, bool IsOnline, bool WasOnline);

/// <summary>
/// Decides whether a fresh ping result is a genuine online/offline
/// transition, given the previous known result. Kept as a small, dependency-free
/// service (not a MediatR handler) so both the Worker's ping loop and any
/// future backfill/reprocessing job can reuse the exact same rule.
/// </summary>
public class DeviceStatusEvaluator
{
    /// <param name="previous">The last known ping result for this device, or null if never checked before.</param>
    /// <param name="isOnlineNow">Result of the ping just performed.</param>
    public StatusEvaluation Evaluate(PingResult? previous, bool isOnlineNow)
    {
        var wasOnline = previous?.IsOnline ?? isOnlineNow; // treat "no history" as no transition
        var statusChanged = previous is not null && wasOnline != isOnlineNow;

        return new StatusEvaluation(statusChanged, isOnlineNow, wasOnline);
    }
}
