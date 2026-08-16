using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Application.Services;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;
using NetworkMonitor.Infrastructure.Logging;

namespace NetworkMonitor.Worker.Alerts;

/// <summary>
/// Evaluates every applicable AlertRule for a device against a fresh
/// ping/SNMP result, dispatches through whichever channel each rule
/// specifies, logs an AlertSent event, and pushes a live notification.
/// This is intentionally the *only* place that ties AlertEvaluationService
/// (pure logic, in Application) together with dispatch/logging/realtime
/// side effects — PingBackgroundService and SnmpBackgroundService just
/// call one method here after each sweep and don't know how alerting works.
///
/// Also owns re-notify rate limiting: AlertEvaluationService's job is only
/// "is this rule currently breached," which stays true on every tick for as
/// long as a device is down. Without a cooldown here, that would mean a
/// fresh email/Telegram message every 30 seconds for the entire outage —
/// this engine checks AlertRule.LastTriggeredAt against
/// MonitoringWorkerOptions.AlertReNotifyCooldownMinutes before actually
/// dispatching again.
/// </summary>
public class AlertThresholdEngine
{
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly IAlertDispatcherResolver _dispatcherResolver;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly AlertEvaluationService _evaluationService;
    private readonly IDateTimeProvider _clock;
    private readonly MonitoringWorkerOptions _options;
    private readonly ILogger<AlertThresholdEngine> _logger;

    public AlertThresholdEngine(
        IAlertRuleRepository alertRuleRepository,
        IAlertDispatcherResolver dispatcherResolver,
        IEventLogRepository eventLogRepository,
        IRealtimeNotifier realtimeNotifier,
        AlertEvaluationService evaluationService,
        IDateTimeProvider clock,
        IOptions<MonitoringWorkerOptions> options,
        ILogger<AlertThresholdEngine> logger)
    {
        _alertRuleRepository = alertRuleRepository;
        _dispatcherResolver = dispatcherResolver;
        _eventLogRepository = eventLogRepository;
        _realtimeNotifier = realtimeNotifier;
        _evaluationService = evaluationService;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EvaluateAvailabilityAsync(Device device, int consecutiveOfflineCount, CancellationToken cancellationToken)
    {
        var rules = await _alertRuleRepository.GetApplicableRulesAsync(device.Id, cancellationToken);
        var availabilityRules = rules.Where(r => r.Metric == "Availability");

        foreach (var rule in availabilityRules)
        {
            var message = _evaluationService.EvaluateAvailability(rule, device, consecutiveOfflineCount);
            if (message is not null && !IsWithinCooldown(rule))
            {
                await DispatchAndRecordAsync(rule, message, cancellationToken);
            }
        }
    }

    public async Task EvaluateResourceThresholdAsync(
        Device device, MetricKind metric, double currentValue, int consecutiveBreachCount, CancellationToken cancellationToken)
    {
        var metricName = metric == MetricKind.Cpu ? "Cpu" : "Memory";

        var rules = await _alertRuleRepository.GetApplicableRulesAsync(device.Id, cancellationToken);
        var resourceRules = rules.Where(r => r.Metric == metricName);

        foreach (var rule in resourceRules)
        {
            var message = _evaluationService.EvaluateResourceThreshold(rule, device, metric, currentValue, consecutiveBreachCount);
            if (message is not null && !IsWithinCooldown(rule))
            {
                await DispatchAndRecordAsync(rule, message, cancellationToken);
            }
        }
    }

    private bool IsWithinCooldown(AlertRule rule)
    {
        if (rule.LastTriggeredAt is null)
        {
            return false;
        }

        var cooldown = TimeSpan.FromMinutes(_options.AlertReNotifyCooldownMinutes);
        return _clock.UtcNow - rule.LastTriggeredAt.Value < cooldown;
    }

    private async Task DispatchAndRecordAsync(AlertRule rule, AlertMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogEvents.AlertEvaluated, "Alert rule {RuleId} ({RuleName}) breached for device {DeviceId}", rule.Id, rule.Name, rule.DeviceId);

        var sent = await _dispatcherResolver.DispatchAsync(message, cancellationToken);

        if (sent)
        {
            _logger.LogInformation(LogEvents.AlertDispatched, "Alert rule {RuleId} dispatched via {Channel} to {Destination}", rule.Id, rule.Channel, rule.Destination);
        }
        else
        {
            _logger.LogWarning(LogEvents.AlertDispatchFailed, "Alert rule {RuleId} failed to dispatch via {Channel}", rule.Id, rule.Channel);
        }

        var now = _clock.UtcNow;

        // Metadata shape here MUST match what
        // AlertRuleQueries.ParseAlertMetadata (Application layer) expects —
        // { ruleId, ruleName, channel } — since that's what
        // GET /api/alerts/history reconstructs its rows from.
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            ruleId = rule.Id,
            ruleName = rule.Name,
            channel = rule.Channel.ToString()
        });

        await _eventLogRepository.AddAsync(new EventLogEntry
        {
            DeviceId = message.DeviceId,
            EventType = EventType.AlertSent,
            Message = message.Body,
            Metadata = metadataJson,
            OccurredAt = now
        }, cancellationToken);

        rule.LastTriggeredAt = now;
        _alertRuleRepository.Update(rule);

        // One SaveChanges for both the event log insert and the rule
        // update — both repositories share the same AppDbContext instance
        // within this scope (see DependencyInjection.AddInfrastructure,
        // both are AddScoped), so either call would flush both changes;
        // calling it once here keeps that implicit sharing from being a
        // point of confusion later.
        await _eventLogRepository.SaveChangesAsync(cancellationToken);

        if (message.DeviceId is not null)
        {
            await _realtimeNotifier.NotifyAlertTriggeredAsync(
                new AlertTriggeredNotification(rule.Id, message.DeviceId, rule.Metric, message.Body, now),
                cancellationToken);
        }
    }
}
