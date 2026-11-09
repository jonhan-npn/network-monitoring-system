using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NetworkMonitor.API.Hubs;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.API.Controllers;

/// <summary>
/// Internal service-to-service endpoint the Worker posts to via
/// <see cref="HttpRealtimeNotifier"/>. It fans the payload out to connected
/// Angular clients through <see cref="MonitoringHub"/>.
///
/// Deliberately NOT protected by a user JWT: the Worker is a trusted
/// internal service, not a logged-in user. It authenticates with a shared
/// secret (X-Internal-Secret, from Realtime:InternalSecret) and is only
/// reachable on the private Docker network — the Nginx config never
/// proxies /internal/*.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("internal/notifications")]
public class InternalNotificationsController(
    IHubContext<MonitoringHub, IMonitoringClient> hub,
    IConfiguration configuration,
    ILogger<InternalNotificationsController> logger) : ControllerBase
{
    private const string SecretHeader = "X-Internal-Secret";
    private const string DashboardGroup = "dashboard";

    private readonly string _expectedSecret = configuration["Realtime:InternalSecret"] ?? string.Empty;

    [HttpPost("device-status")]
    public async Task<IActionResult> DeviceStatus(
        [FromBody] DeviceStatusChangedNotification notification, CancellationToken ct)
    {
        if (!IsTrustedCaller())
        {
            return Unauthorized();
        }

        var payload = new DeviceStatusUpdate(
            notification.DeviceId,
            notification.DeviceName,
            notification.IsOnline,
            ToUtc(notification.CheckedAt),
            notification.LatencyMs);

        await Task.WhenAll(
            hub.Clients.Group(DashboardGroup).DeviceStatusChanged(payload),
            hub.Clients.Group(DeviceGroup(notification.DeviceId)).DeviceStatusChanged(payload));

        return Accepted();
    }

    [HttpPost("resource-sample")]
    public async Task<IActionResult> ResourceSample(
        [FromBody] ResourceSampleReceivedNotification notification, CancellationToken ct)
    {
        if (!IsTrustedCaller())
        {
            return Unauthorized();
        }

        var payload = new ResourceSampleUpdate(
            notification.DeviceId,
            ToUtc(notification.SampledAt),
            notification.CpuPercent,
            notification.MemoryPercent);

        await Task.WhenAll(
            hub.Clients.Group(DashboardGroup).ResourceSampleReceived(payload),
            hub.Clients.Group(DeviceGroup(notification.DeviceId)).ResourceSampleReceived(payload));

        return Accepted();
    }

    [HttpPost("alert-triggered")]
    public async Task<IActionResult> AlertTriggered(
        [FromBody] AlertTriggeredNotification notification, CancellationToken ct)
    {
        if (!IsTrustedCaller())
        {
            return Unauthorized();
        }

        var payload = new AlertTriggeredUpdate(
            notification.RuleId,
            notification.DeviceId,
            notification.Metric,
            notification.Message,
            ToUtc(notification.TriggeredAt));

        if (notification.DeviceId is { } deviceId)
        {
            await Task.WhenAll(
                hub.Clients.Group(DashboardGroup).AlertTriggered(payload),
                hub.Clients.Group(DeviceGroup(deviceId)).AlertTriggered(payload));
        }
        else
        {
            await hub.Clients.Group(DashboardGroup).AlertTriggered(payload);
        }

        return Accepted();
    }

    private bool IsTrustedCaller()
    {
        if (string.IsNullOrEmpty(_expectedSecret))
        {
            // Fail closed: without a configured secret this endpoint would be
            // an open notification relay on the internal network.
            logger.LogWarning("Realtime:InternalSecret is not configured; rejecting internal notification.");
            return false;
        }

        if (!Request.Headers.TryGetValue(SecretHeader, out var providedValues))
        {
            return false;
        }

        var expected = System.Text.Encoding.UTF8.GetBytes(_expectedSecret);
        var provided = System.Text.Encoding.UTF8.GetBytes(providedValues.ToString());

        return expected.Length == provided.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    private static string DeviceGroup(Guid deviceId) => $"device:{deviceId}";

    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
