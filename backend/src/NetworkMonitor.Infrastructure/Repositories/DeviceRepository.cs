using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;
using NetworkMonitor.Infrastructure.Data;

namespace NetworkMonitor.Infrastructure.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _db;

    public DeviceRepository(AppDbContext db) => _db = db;

    public Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Device?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default) =>
        _db.Devices.FirstOrDefaultAsync(d => d.IpAddress == ipAddress, cancellationToken);

    public async Task<(IReadOnlyList<Device> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        DeviceType? deviceType,
        bool? isMonitored,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Devices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILIKE-style case-insensitive search via EF.Functions, translates
            // to Postgres's native ILIKE rather than pulling rows into memory.
            query = query.Where(d =>
                EF.Functions.ILike(d.Name, $"%{search}%") ||
                EF.Functions.ILike(d.IpAddress, $"%{search}%"));
        }

        if (deviceType is not null)
        {
            query = query.Where(d => d.DeviceType == deviceType);
        }

        if (isMonitored is not null)
        {
            query = query.Where(d => d.IsMonitored == isMonitored);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Device>> GetAllMonitoredAsync(CancellationToken cancellationToken = default) =>
        await _db.Devices
            .AsNoTracking()
            .Where(d => d.IsMonitored)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Device device, CancellationToken cancellationToken = default) =>
        await _db.Devices.AddAsync(device, cancellationToken);

    public void Update(Device device) => _db.Devices.Update(device);

    public void Remove(Device device) => _db.Devices.Remove(device);

    public Task<bool> ExistsWithIpAsync(string ipAddress, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Devices.Where(d => d.IpAddress == ipAddress);

        if (excludeId is not null)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
