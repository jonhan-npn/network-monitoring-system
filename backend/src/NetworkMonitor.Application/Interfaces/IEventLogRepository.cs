using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Interfaces;

public interface IEventLogRepository
{
    Task AddAsync(EventLogEntry entry, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EventLogEntry> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? deviceId,
        EventType? eventType,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
