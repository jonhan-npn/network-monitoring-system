namespace NetworkMonitor.Application.Interfaces;

public record DeviceStatusChangedNotification(Guid DeviceId, bool IsOnline, DateTime CheckedAt);
public record ResourceSampleReceivedNotification(Guid DeviceId, double? CpuPercent, double? MemoryPercent, DateTime SampledAt);
public record AlertTriggeredNotification(Guid RuleId, Guid? DeviceId, string Metric, string Message, DateTime TriggeredAt);

/// <summary>
/// Push notifications to connected dashboard clients. Application and the
/// Worker depend only on this interface — the actual SignalR
/// IHubContext&lt;MonitoringHub&gt; usage lives in the API project, which
/// implements this interface and is referenced by the Worker only as a
/// compiled contract (via Infrastructure), keeping Worker decoupled from
/// ASP.NET Core hosting concerns.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyDeviceStatusChangedAsync(DeviceStatusChangedNotification notification, CancellationToken cancellationToken = default);
    Task NotifyResourceSampleReceivedAsync(ResourceSampleReceivedNotification notification, CancellationToken cancellationToken = default);
    Task NotifyAlertTriggeredAsync(AlertTriggeredNotification notification, CancellationToken cancellationToken = default);
}
