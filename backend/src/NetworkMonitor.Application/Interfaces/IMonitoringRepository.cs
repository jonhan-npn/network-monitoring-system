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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
