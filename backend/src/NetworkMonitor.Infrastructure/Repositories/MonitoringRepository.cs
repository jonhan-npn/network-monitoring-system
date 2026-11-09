using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Infrastructure.Data;

namespace NetworkMonitor.Infrastructure.Repositories;

public class MonitoringRepository : IMonitoringRepository
{
    private readonly AppDbContext _db;

    public MonitoringRepository(AppDbContext db) => _db = db;

    // ---- Writes ----

    public async Task AddPingResultAsync(PingResult result, CancellationToken cancellationToken = default) =>
        await _db.PingResults.AddAsync(result, cancellationToken);

    public async Task AddResourceSampleAsync(ResourceSample sample, CancellationToken cancellationToken = default) =>
        await _db.ResourceSamples.AddAsync(sample, cancellationToken);

    public Task<PingResult?> GetLatestPingResultAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
        _db.PingResults
            .AsNoTracking()
            .Where(p => p.DeviceId == deviceId)
            .OrderByDescending(p => p.CheckedAt)
            .FirstOrDefaultAsync(cancellationToken);

    // ---- Reads ----

    public async Task<IReadOnlyDictionary<Guid, PingResult>> GetLatestPingResultsAsync(
        IReadOnlyList<Guid> deviceIds, CancellationToken cancellationToken = default)
    {
        if (deviceIds.Count == 0)
        {
            return new Dictionary<Guid, PingResult>();
        }

        // DISTINCT ON would be the idiomatic Postgres way to write this, but
        // EF Core's LINQ provider doesn't expose it directly. GroupBy +
        // MaxBy translates to a reasonably efficient query for the device
        // counts this project targets (tens to low hundreds); if this ever
        // becomes a bottleneck at higher device counts, replace with a raw
        // `SELECT DISTINCT ON (device_id) ...` via FromSqlRaw.
        var latestPerDevice = await _db.PingResults
            .AsNoTracking()
            .Where(p => deviceIds.Contains(p.DeviceId))
            .GroupBy(p => p.DeviceId)
            .Select(g => g.OrderByDescending(p => p.CheckedAt).First())
            .ToListAsync(cancellationToken);

        return latestPerDevice.ToDictionary(p => p.DeviceId);
    }

    public async Task<IReadOnlyList<PingResult>> GetPingHistoryAsync(
        Guid deviceId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        await _db.PingResults
            .AsNoTracking()
            .Where(p => p.DeviceId == deviceId && p.CheckedAt >= from && p.CheckedAt <= to)
            .OrderBy(p => p.CheckedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ResourceSample>> GetResourceHistoryAsync(
        Guid deviceId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        await _db.ResourceSamples
            .AsNoTracking()
            .Where(r => r.DeviceId == deviceId && r.SampledAt >= from && r.SampledAt <= to)
            .OrderBy(r => r.SampledAt)
            .ToListAsync(cancellationToken);

    public Task<ResourceSample?> GetLatestResourceSampleAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
        _db.ResourceSamples
            .AsNoTracking()
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.SampledAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, ResourceSample>> GetLatestResourceSamplesAsync(
        IReadOnlyList<Guid> deviceIds, CancellationToken cancellationToken = default)
    {
        if (deviceIds.Count == 0)
        {
            return new Dictionary<Guid, ResourceSample>();
        }

        // Same GroupBy/MaxBy strategy as GetLatestPingResultsAsync above.
        var latestPerDevice = await _db.ResourceSamples
            .AsNoTracking()
            .Where(r => deviceIds.Contains(r.DeviceId))
            .GroupBy(r => r.DeviceId)
            .Select(g => g.OrderByDescending(r => r.SampledAt).First())
            .ToListAsync(cancellationToken);

        return latestPerDevice.ToDictionary(r => r.DeviceId);
    }

    // ---- Retention ----

    public Task<int> PurgeOldPingResultsAsync(DateTime olderThan, CancellationToken cancellationToken = default) =>
        // ExecuteDeleteAsync issues a single DELETE statement in the
        // database rather than loading rows into memory to remove them —
        // essential given the row volumes discussed in docs/database.md.
        _db.PingResults
            .Where(p => p.CheckedAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> PurgeOldResourceSamplesAsync(DateTime olderThan, CancellationToken cancellationToken = default) =>
        _db.ResourceSamples
            .Where(r => r.SampledAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
