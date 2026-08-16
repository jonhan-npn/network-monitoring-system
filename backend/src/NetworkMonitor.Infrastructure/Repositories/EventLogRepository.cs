using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;
using NetworkMonitor.Infrastructure.Data;

namespace NetworkMonitor.Infrastructure.Repositories;

public class EventLogRepository : IEventLogRepository
{
    private readonly AppDbContext _db;

    public EventLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(EventLogEntry entry, CancellationToken cancellationToken = default) =>
        await _db.EventLogEntries.AddAsync(entry, cancellationToken);

    public async Task<(IReadOnlyList<EventLogEntry> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? deviceId,
        EventType? eventType,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _db.EventLogEntries.AsNoTracking().Include(e => e.Device).AsQueryable();

        if (deviceId is not null) query = query.Where(e => e.DeviceId == deviceId);
        if (eventType is not null) query = query.Where(e => e.EventType == eventType);
        if (from is not null) query = query.Where(e => e.OccurredAt >= from);
        if (to is not null) query = query.Where(e => e.OccurredAt <= to);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
