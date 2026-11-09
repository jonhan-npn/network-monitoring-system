using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Interfaces;

public interface IMonitoringRepository
{
    // ---- Writes (called by the Worker after each sweep) ----
    Task AddPingResultAsync(PingResult result, CancellationToken cancellationToken = default);
    Task AddResourceSampleAsync(ResourceSample sample, CancellationToken cancellationToken = default);

    /// <summary>Latest ping result per device, used to detect online/offline transitions.</summary>
    Task<PingResult?> GetLatestPingResultAsync(Guid deviceId, CancellationToken cancellationToken = default);

    // ---- Reads (called by the API for dashboard/history) ----
    Task<IReadOnlyDictionary<Guid, PingResult>> GetLatestPingResultsAsync(
        IReadOnlyList<Guid> deviceIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PingResult>> GetPingHistoryAsync(
        Guid deviceId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceSample>> GetResourceHistoryAsync(
        Guid deviceId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<ResourceSample?> GetLatestResourceSampleAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Latest resource sample per device, in one round trip. Used by the
    /// dashboard summary so it doesn't fan out N concurrent queries against
    /// the same (non-thread-safe) DbContext.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ResourceSample>> GetLatestResourceSamplesAsync(
        IReadOnlyList<Guid> deviceIds, CancellationToken cancellationToken = default);

    // ---- Retention (called by the Worker's DataRetentionJob) ----
    Task<int> PurgeOldPingResultsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task<int> PurgeOldResourceSamplesAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
