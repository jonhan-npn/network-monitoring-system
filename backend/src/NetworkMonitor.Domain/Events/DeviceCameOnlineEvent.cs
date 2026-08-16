using NetworkMonitor.Domain.Common;

namespace NetworkMonitor.Domain.Events;

public class DeviceCameOnlineEvent : IDomainEvent
{
    public Guid DeviceId { get; }
    public string DeviceName { get; }
    public DateTime OccurredAt { get; }

    public DeviceCameOnlineEvent(Guid deviceId, string deviceName, DateTime occurredAt)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        OccurredAt = occurredAt;
    }
}
