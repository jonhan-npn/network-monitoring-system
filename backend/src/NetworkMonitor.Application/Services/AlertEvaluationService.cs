using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Services;

public enum MetricKind
{
    Availability,
    Cpu,
    Memory
}

/// <summary>
/// Given an alert rule and the current metric value(s), decides whether the
/// rule's threshold has been breached and — if so — builds the message to
/// dispatch. Deliberately has no knowledge of email/Telegram/SignalR; it
/// only produces an AlertMessage that IAlertDispatcherResolver can send
/// wherever the rule's Channel points.
/// </summary>
public class AlertEvaluationService
{
    /// <summary>
    /// Evaluates an Availability rule against consecutive offline pings.
    /// </summary>
    public AlertMessage? EvaluateAvailability(AlertRule rule, Device device, int consecutiveOfflineCount)
    {
        if (!rule.IsEnabled || consecutiveOfflineCount < rule.ConsecutiveBreaches)
        {
            return null;
        }

        return BuildMessage(
            rule,
            device,
            subject: $"[Network Monitor] {device.Name} is offline",
            body: $"{device.Name} ({device.IpAddress}) has failed {consecutiveOfflineCount} consecutive ping checks.");
    }

    /// <summary>
    /// Evaluates a Cpu/Memory rule against a single fresh sample and a
    /// count of how many consecutive samples have already breached it.
    /// </summary>
    public AlertMessage? EvaluateResourceThreshold(
        AlertRule rule,
        Device device,
        MetricKind metric,
        double currentValue,
        int consecutiveBreachCount)
    {
        if (!rule.IsEnabled || rule.ThresholdPercent is null)
        {
            return null;
        }

        if (currentValue < rule.ThresholdPercent || consecutiveBreachCount < rule.ConsecutiveBreaches)
        {
            return null;
        }

        var metricLabel = metric == MetricKind.Cpu ? "CPU" : "memory";

        return BuildMessage(
            rule,
            device,
            subject: $"[Network Monitor] {device.Name} — high {metricLabel} usage",
            body: $"{device.Name} ({device.IpAddress}) {metricLabel} usage is {currentValue:F1}%, " +
                  $"above the {rule.ThresholdPercent:F0}% threshold for {consecutiveBreachCount} consecutive samples.");
    }

    private static AlertMessage BuildMessage(AlertRule rule, Device device, string subject, string body) =>
        new(
            RuleId: rule.Id,
            DeviceId: device.Id,
            DeviceName: device.Name,
            Subject: subject,
            Body: body,
            Channel: rule.Channel,
            Destination: rule.Destination);
}
