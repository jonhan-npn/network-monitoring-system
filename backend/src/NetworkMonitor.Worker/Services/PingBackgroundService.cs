using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Application.Features.Monitoring.Commands;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Infrastructure.Logging;
using NetworkMonitor.Worker.Alerts;
using NetworkMonitor.Worker.Monitoring;

namespace NetworkMonitor.Worker.Services;

/// <summary>
/// The core "ping every device every 30 seconds" loop. Uses PeriodicTimer
/// (not Thread.Sleep) so cancellation is observed promptly on shutdown —
/// see docs/deployment.md, "Why PeriodicTimer, not Thread.Sleep."
///
/// Creates a new DI scope per tick because AppDbContext (via the
/// repositories) is scoped, and a single long-lived scope for the whole
/// Worker lifetime would mean one DbContext instance accumulating tracked
/// entities forever — a classic background-service memory leak.
/// </summary>
public class PingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsecutiveCounterTracker _counters;
    private readonly MonitoringWorkerOptions _options;
    private readonly ILogger<PingBackgroundService> _logger;

    private const string AvailabilityMetricKey = "Availability";

    public PingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ConsecutiveCounterTracker counters,
        IOptions<MonitoringWorkerOptions> options,
        ILogger<PingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _counters = counters;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PingIntervalSeconds));

        // Run once immediately on startup rather than waiting a full
        // interval for the first sweep — otherwise the dashboard shows
        // "no data" for up to 30 seconds after every deploy for no reason.
        await RunSweepAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSweepAsync(stoppingToken);
        }
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var deviceRepository = services.GetRequiredService<IDeviceRepository>();
        var pingService = services.GetRequiredService<IPingService>();
        var mediator = services.GetRequiredService<IMediator>();
        var alertEngine = services.GetRequiredService<AlertThresholdEngine>();

        try
        {
            var devices = await deviceRepository.GetAllMonitoredAsync(cancellationToken);
            if (devices.Count == 0)
            {
                return;
            }

            _logger.LogDebug(LogEvents.PingSweepStarted, "Ping sweep starting for {Count} devices", devices.Count);

            var targets = devices.Select(d => (d.Id, d.IpAddress)).ToList();
            var results = await pingService.PingManyAsync(targets, _options.PingTimeoutMs, cancellationToken);

            var deviceLookup = devices.ToDictionary(d => d.Id);

            foreach (var (deviceId, result) in results)
            {
                var device = deviceLookup[deviceId];

                var recordResult = await mediator.Send(
                    new RecordPingResultCommand(deviceId, device.Name, result.IsOnline, result.LatencyMs, result.CheckedAt),
                    cancellationToken);

                if (recordResult.StatusChanged)
                {
                    _logger.LogInformation(
                        LogEvents.DeviceStatusChanged,
                        "Device {DeviceId} ({DeviceName}) is now {Status}",
                        deviceId, device.Name, result.IsOnline ? "online" : "offline");
                }

                if (result.IsOnline)
                {
                    _counters.Reset(deviceId, AvailabilityMetricKey);
                }
                else
                {
                    var streak = _counters.Increment(deviceId, AvailabilityMetricKey);
                    await alertEngine.EvaluateAvailabilityAsync(device, streak, cancellationToken);
                }
            }

            _logger.LogDebug(LogEvents.PingSweepCompleted, "Ping sweep completed for {Count} devices", devices.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A single bad sweep should never kill the hosted service —
            // BackgroundService.ExecuteAsync throwing stops the timer loop
            // entirely, which would mean no more pings until the whole
            // container restarts. Log and let the next tick try again.
            _logger.LogError(ex, "Ping sweep failed unexpectedly");
        }
    }
}
