using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Device?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Device> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        DeviceType? deviceType,
        bool? isMonitored,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Device>> GetAllMonitoredAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Device device, CancellationToken cancellationToken = default);
    void Update(Device device);
    void Remove(Device device);

    Task<bool> ExistsWithIpAsync(string ipAddress, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Persists pending changes for this unit of work.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
