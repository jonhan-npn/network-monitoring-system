using NetworkMonitor.Domain.Common;

namespace NetworkMonitor.Domain.Events;

public class DeviceWentOfflineEvent : IDomainEvent
{
    public Guid DeviceId { get; }
    public string DeviceName { get; }
    public DateTime OccurredAt { get; }

    public DeviceWentOfflineEvent(Guid deviceId, string deviceName, DateTime occurredAt)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        OccurredAt = occurredAt;
    }
}
