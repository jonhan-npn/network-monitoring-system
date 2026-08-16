namespace NetworkMonitor.Domain.Common;

/// <summary>
/// Optional base for future GUID-keyed entities that want Id/CreatedAt/UpdatedAt
/// for free. Device, User, and AlertRule do NOT inherit from this today —
/// they declare those three properties directly with public setters, since
/// Application-layer handlers assign them straight from mapped request data
/// (e.g. <c>device.UpdatedAt = _clock.UtcNow;</c> in UpdateDeviceCommandHandler)
/// and a base class buys nothing extra there. Reach for this if you add a new
/// GUID-keyed entity and want to avoid repeating the three properties.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
