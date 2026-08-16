using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Application.Features.Monitoring.Commands;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Application.Services;
using NetworkMonitor.Infrastructure.Logging;
using NetworkMonitor.Worker.Alerts;
using NetworkMonitor.Worker.Monitoring;

namespace NetworkMonitor.Worker.Services;

/// <summary>
/// SNMP CPU/RAM polling loop, extending the same PeriodicTimer pattern as
/// PingBackgroundService but on its own (typically longer) interval — see
/// MonitoringWorkerOptions.SnmpIntervalSeconds. Runs independently of the
/// ping sweep so a slow/unreachable SNMP agent on one device can't delay
/// ping checks for every other device.
/// </summary>
public class SnmpBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsecutiveCounterTracker _counters;
    private readonly MonitoringWorkerOptions _options;
    private readonly ILogger<SnmpBackgroundService> _logger;

    private const string CpuMetricKey = "Cpu";
    private const string MemoryMetricKey = "Memory";

    public SnmpBackgroundService(
        IServiceScopeFactory scopeFactory,
        ConsecutiveCounterTracker counters,
        IOptions<MonitoringWorkerOptions> options,
        ILogger<SnmpBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _counters = counters;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.SnmpIntervalSeconds));

        await RunPollAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunPollAsync(stoppingToken);
        }
    }

    private async Task RunPollAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var deviceRepository = services.GetRequiredService<IDeviceRepository>();
        var alertRuleRepository = services.GetRequiredService<IAlertRuleRepository>();
        var snmpService = services.GetRequiredService<ISnmpService>();
        var mediator = services.GetRequiredService<IMediator>();
        var alertEngine = services.GetRequiredService<AlertThresholdEngine>();

        try
        {
            var devices = await deviceRepository.GetAllMonitoredAsync(cancellationToken);
            if (devices.Count == 0)
            {
                return;
            }

            _logger.LogDebug(LogEvents.SnmpPollStarted, "SNMP poll starting for {Count} devices", devices.Count);

            var targets = devices
                .Select(d => new SnmpTarget(d.Id, d.IpAddress, d.SnmpCommunity, d.SnmpPort, d.SnmpVersion))
                .ToList();

            var results = await snmpService.PollManyAsync(targets, cancellationToken);
            var deviceLookup = devices.ToDictionary(d => d.Id);

            foreach (var (deviceId, result) in results)
            {
                if (!result.Success)
                {
                    // Not every device type will run an SNMP agent — a
                    // failed poll here is routine, not exceptional; already
                    // logged at Debug inside SnmpPollingService.
                    continue;
                }

                var device = deviceLookup[deviceId];

                await mediator.Send(
                    new RecordResourceSampleCommand(deviceId, result.CpuPercent, result.MemoryPercent, result.SampledAt),
                    cancellationToken);

                if (result.CpuPercent is { } cpu)
                {
                    await EvaluateMetricAsync(device, MetricKind.Cpu, CpuMetricKey, cpu, alertRuleRepository, alertEngine, cancellationToken);
                }

                if (result.MemoryPercent is { } memory)
                {
                    await EvaluateMetricAsync(device, MetricKind.Memory, MemoryMetricKey, memory, alertRuleRepository, alertEngine, cancellationToken);
                }
            }

            _logger.LogDebug(LogEvents.SnmpPollCompleted, "SNMP poll completed for {Count} devices", devices.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SNMP poll failed unexpectedly");
        }
    }

    /// <summary>
    /// Tracks a consecutive-breach streak against the LOWEST threshold
    /// configured across this device's rules for the metric, then lets
    /// AlertThresholdEngine's own per-rule check (currentValue vs. that
    /// specific rule's ThresholdPercent) decide which individual rules
    /// actually fire. Simplification: if you configure two rules on the
    /// same metric with very different thresholds (e.g. Cpu > 80 and
    /// Cpu > 95), a streak that only ever breaches the lower one won't
    /// inflate the higher rule's streak incorrectly — EvaluateResourceThreshold
    /// still gates on `currentValue >= rule.ThresholdPercent` — but a value
    /// oscillating around the lower threshold can make the streak count
    /// look choppier than either rule's actual breach history. Precise
    /// per-rule streak tracking would need per-rule counters instead of
    /// per-metric ones; not worth the added complexity for a learning build.
    /// </summary>
    private async Task EvaluateMetricAsync(
        Domain.Entities.Device device,
        MetricKind metric,
        string metricKey,
        double currentValue,
        IAlertRuleRepository alertRuleRepository,
        AlertThresholdEngine alertEngine,
        CancellationToken cancellationToken)
    {
        var metricName = metric == MetricKind.Cpu ? "Cpu" : "Memory";
        var rules = await alertRuleRepository.GetApplicableRulesAsync(device.Id, cancellationToken);
        var relevantRules = rules.Where(r => r.Metric == metricName && r.ThresholdPercent is not null).ToList();

        if (relevantRules.Count == 0)
        {
            return;
        }

        var lowestThreshold = relevantRules.Min(r => r.ThresholdPercent!.Value);

        int streak;
        if (currentValue >= lowestThreshold)
        {
            streak = _counters.Increment(device.Id, metricKey);
        }
        else
        {
            _counters.Reset(device.Id, metricKey);
            streak = 0;
        }

        if (streak > 0)
        {
            await alertEngine.EvaluateResourceThresholdAsync(device, metric, currentValue, streak, cancellationToken);
        }
    }
}
