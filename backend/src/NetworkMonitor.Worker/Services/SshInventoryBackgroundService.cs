using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;
using NetworkMonitor.Infrastructure.Logging;

namespace NetworkMonitor.Worker.Services;

/// <summary>
/// Periodically pulls switch/router inventory (hostname, firmware version,
/// serial number) over SSH. Runs far less often than ping/SNMP — this data
/// doesn't change minute to minute, so MonitoringWorkerOptions.SshInventoryIntervalMinutes
/// defaults to 30 rather than 30 seconds.
///
/// Results are written as a DeviceUpdated EventLogEntry rather than onto
/// the Device row itself — the current Device entity/schema doesn't have
/// Hostname/Firmware/Serial columns (see docs/database.md). If you want
/// those queryable as first-class device fields rather than buried in an
/// event log's JSON metadata, that's a small, deliberate schema addition
/// left for you to make once you've decided you actually want it — adding
/// it up front would be exactly the kind of "overwhelming" scope creep the
/// build order in the README is trying to avoid.
/// </summary>
public class SshInventoryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringWorkerOptions _options;
    private readonly ILogger<SshInventoryBackgroundService> _logger;

    public SshInventoryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringWorkerOptions> options,
        ILogger<SshInventoryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.SshInventoryIntervalMinutes));

        // Unlike ping/SNMP, don't run this immediately on startup — SSH
        // inventory is comparatively expensive (one interactive-ish
        // connection per device) and isn't needed for the dashboard to be
        // useful the instant the Worker comes up. Wait for the first tick.
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunInventorySweepAsync(stoppingToken);
        }
    }

    private async Task RunInventorySweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var deviceRepository = services.GetRequiredService<IDeviceRepository>();
        var eventLogRepository = services.GetRequiredService<IEventLogRepository>();
        var sshService = services.GetRequiredService<ISshService>();
        var clock = services.GetRequiredService<IDateTimeProvider>();

        try
        {
            var devices = await deviceRepository.GetAllMonitoredAsync(cancellationToken);

            var sshCapableDevices = devices
                .Where(d => !string.IsNullOrWhiteSpace(d.SshUsername) && !string.IsNullOrWhiteSpace(d.SshCredentialRef))
                .ToList();

            if (sshCapableDevices.Count == 0)
            {
                return;
            }

            _logger.LogDebug(LogEvents.SshInventoryStarted, "SSH inventory sweep starting for {Count} devices", sshCapableDevices.Count);

            foreach (var device in sshCapableDevices)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await FetchAndLogInventoryAsync(device, sshService, eventLogRepository, clock, cancellationToken);
            }

            await eventLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(LogEvents.SshInventoryCompleted, "SSH inventory sweep completed for {Count} devices", sshCapableDevices.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SSH inventory sweep failed unexpectedly");
        }
    }

    private async Task FetchAndLogInventoryAsync(
        Device device,
        ISshService sshService,
        IEventLogRepository eventLogRepository,
        IDateTimeProvider clock,
        CancellationToken cancellationToken)
    {
        var target = new SshTarget(device.Id, device.IpAddress, device.SshPort, device.SshUsername!, device.SshCredentialRef!);

        // Per-device failures are expected and routine (device offline,
        // wrong credentials, unsupported command) — never let one bad
        // device abort the sweep for the rest.
        var result = await sshService.FetchInventoryAsync(target, cancellationToken);

        if (!result.Success)
        {
            _logger.LogDebug("SSH inventory failed for device {DeviceId} ({DeviceName}): {Error}", device.Id, device.Name, result.ErrorMessage);
            return;
        }

        var metadata = JsonSerializer.Serialize(new
        {
            hostname = result.Hostname,
            firmwareVersion = result.FirmwareVersion,
            serialNumber = result.SerialNumber
        });

        await eventLogRepository.AddAsync(new EventLogEntry
        {
            DeviceId = device.Id,
            EventType = EventType.DeviceUpdated,
            Message = $"SSH inventory refreshed for {device.Name}.",
            Metadata = metadata,
            OccurredAt = clock.UtcNow
        }, cancellationToken);
    }
}
