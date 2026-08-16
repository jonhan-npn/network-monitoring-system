using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Infrastructure.Data;

namespace NetworkMonitor.Infrastructure.Repositories;

public class AlertRuleRepository : IAlertRuleRepository
{
    private readonly AppDbContext _db;

    public AlertRuleRepository(AppDbContext db) => _db = db;

    public Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.AlertRules.Include(r => r.Device).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AlertRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.AlertRules
            .AsNoTracking()
            .Include(r => r.Device)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AlertRule>> GetApplicableRulesAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
        await _db.AlertRules
            .AsNoTracking()
            .Where(r => r.IsEnabled && (r.DeviceId == deviceId || r.DeviceId == null))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AlertRule rule, CancellationToken cancellationToken = default) =>
        await _db.AlertRules.AddAsync(rule, cancellationToken);

    public void Update(AlertRule rule) => _db.AlertRules.Update(rule);

    public void Remove(AlertRule rule) => _db.AlertRules.Remove(rule);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
