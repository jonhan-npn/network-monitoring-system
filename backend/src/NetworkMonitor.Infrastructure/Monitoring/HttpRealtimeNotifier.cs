using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Infrastructure.Monitoring;

/// <summary>Bound from "Realtime" — see RealtimeNotifierSettings.InternalApiBaseUrl / InternalSecret below.</summary>
public class RealtimeNotifierSettings
{
    public const string SectionName = "Realtime";

    /// <summary>Base URL of the API container reachable from the Worker,
    /// e.g. "http://api:8080" over the internal Docker network — see
    /// docs/deployment.md for why only the frontend's port is published
    /// to the host, while api:8080 stays internal-only and reachable by
    /// name between containers.</summary>
    public string InternalApiBaseUrl { get; set; } = "http://api:8080";

    /// <summary>
    /// Shared secret sent as the X-Internal-Secret header. This is NOT a
    /// user JWT — the Worker isn't acting as any logged-in user, it's a
    /// trusted internal service, so a static shared secret (rotated like
    /// any other credential in .env) is the appropriate mechanism here
    /// rather than issuing the Worker a JWT for a user that doesn't exist.
    /// </summary>
    public string InternalSecret { get; set; } = string.Empty;
}

/// <summary>
/// Implements IRealtimeNotifier by POSTing to a small set of internal-only
/// endpoints the API exposes (e.g. POST /internal/notifications/device-status),
/// which then forward the payload to connected dashboard clients via
/// IHubContext&lt;MonitoringHub&gt;. This is what keeps the Worker decoupled
/// from ASP.NET Core's SignalR hosting types entirely — it only knows HTTP.
///
/// Registered as a typed HttpClient in DependencyInjection.AddInfrastructure;
/// the corresponding API-side controller is
/// NetworkMonitor.API.Controllers.InternalNotificationsController, which
/// validates X-Internal-Secret and forwards each payload to the hub context.
/// </summary>
public class HttpRealtimeNotifier : IRealtimeNotifier
{
    private readonly HttpClient _httpClient;
    private readonly RealtimeNotifierSettings _settings;
    private readonly ILogger<HttpRealtimeNotifier> _logger;

    public HttpRealtimeNotifier(HttpClient httpClient, IOptions<RealtimeNotifierSettings> settings, ILogger<HttpRealtimeNotifier> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_settings.InternalApiBaseUrl);
        }

        if (!string.IsNullOrEmpty(_settings.InternalSecret))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-Internal-Secret");
            _httpClient.DefaultRequestHeaders.Add("X-Internal-Secret", _settings.InternalSecret);
        }
    }

    public Task NotifyDeviceStatusChangedAsync(DeviceStatusChangedNotification notification, CancellationToken cancellationToken = default) =>
        PostFireAndForgetAsync("/internal/notifications/device-status", notification, cancellationToken);

    public Task NotifyResourceSampleReceivedAsync(ResourceSampleReceivedNotification notification, CancellationToken cancellationToken = default) =>
        PostFireAndForgetAsync("/internal/notifications/resource-sample", notification, cancellationToken);

    public Task NotifyAlertTriggeredAsync(AlertTriggeredNotification notification, CancellationToken cancellationToken = default) =>
        PostFireAndForgetAsync("/internal/notifications/alert-triggered", notification, cancellationToken);

    /// <summary>
    /// "Fire and forget" in the sense that a failed push never throws back
    /// into the ping/SNMP loop — a dropped live-update is much cheaper than
    /// stalling the monitoring sweep over a transient API hiccup. The
    /// underlying data is already safely persisted to Postgres by this
    /// point (see RecordPingResultCommandHandler) regardless of whether
    /// this notification succeeds.
    /// </summary>
    private async Task PostFireAndForgetAsync<T>(string path, T payload, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(path, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Realtime notification to {Path} returned {StatusCode}", path, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Realtime notification to {Path} failed", path);
        }
    }
}
