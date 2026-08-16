using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Interfaces;

public interface IAlertRuleRepository
{
    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertRule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Rules that apply to a device: its own rules plus global (DeviceId == null) rules.</summary>
    Task<IReadOnlyList<AlertRule>> GetApplicableRulesAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task AddAsync(AlertRule rule, CancellationToken cancellationToken = default);
    void Update(AlertRule rule);
    void Remove(AlertRule rule);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
