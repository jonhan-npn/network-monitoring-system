using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NetworkMonitor.API.Hubs;

/// <summary>
/// Push channel for live device status. The Worker's background services push
/// updates to this hub (via IHubContext&lt;MonitoringHub&gt;) whenever a ping
/// result or SNMP sample changes a device's state; the Angular dashboard
/// subscribes to receive them without polling.
/// </summary>
[Authorize]
public class MonitoringHub : Hub<IMonitoringClient>
{
    /// <summary>
    /// Clients call this after connecting to join a group for a specific
    /// device, so updates can be scoped to whatever the user has open
    /// (e.g. a device-detail page) instead of broadcasting everything to everyone.
    /// </summary>
    public async Task SubscribeToDevice(string deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(deviceId));
    }

    public async Task UnsubscribeFromDevice(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(deviceId));
    }

    /// <summary>
    /// The dashboard overview page joins this group to receive status
    /// changes for every device without subscribing one-by-one.
    /// </summary>
    public async Task SubscribeToDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // Group membership is cleaned up automatically by SignalR on disconnect.
        return base.OnDisconnectedAsync(exception);
    }

    private static string GroupName(string deviceId) => $"device:{deviceId}";
}

/// <summary>
/// Strongly typed contract for what the server pushes to clients.
/// Use with IHubContext&lt;MonitoringHub, IMonitoringClient&gt; from the Worker
/// so message names/payloads can't drift out of sync with the Angular side.
/// </summary>
public interface IMonitoringClient
{
    Task DeviceStatusChanged(DeviceStatusUpdate update);
    Task ResourceSampleReceived(ResourceSampleUpdate update);
    Task AlertTriggered(AlertTriggeredUpdate alert);
}

public record DeviceStatusUpdate(Guid DeviceId, string Name, bool IsOnline, DateTimeOffset TimestampUtc, double? LatencyMs);

public record ResourceSampleUpdate(Guid DeviceId, DateTimeOffset TimestampUtc, double? CpuPercent, double? RamPercent);

public record AlertTriggeredUpdate(Guid RuleId, Guid? DeviceId, string Metric, string Message, DateTimeOffset TimestampUtc);
