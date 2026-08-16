using Microsoft.Extensions.Logging;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Infrastructure.Notifications;

public class AlertDispatcherResolver : IAlertDispatcherResolver
{
    private readonly IReadOnlyDictionary<Domain.Enums.AlertChannel, IAlertDispatcher> _dispatchers;
    private readonly ILogger<AlertDispatcherResolver> _logger;

    public AlertDispatcherResolver(IEnumerable<IAlertDispatcher> dispatchers, ILogger<AlertDispatcherResolver> logger)
    {
        // Last registration wins if two dispatchers somehow claim the same
        // channel — shouldn't happen with the two registered in
        // DependencyInjection, but ToDictionary would throw on a genuine
        // duplicate, which is the right failure mode (fail at startup, not
        // silently drop alerts at runtime).
        _dispatchers = dispatchers.ToDictionary(d => d.Channel);
        _logger = logger;
    }

    public async Task<bool> DispatchAsync(AlertMessage message, CancellationToken cancellationToken = default)
    {
        if (!_dispatchers.TryGetValue(message.Channel, out var dispatcher))
        {
            _logger.LogError("No IAlertDispatcher registered for channel {Channel} (rule {RuleId})", message.Channel, message.RuleId);
            return false;
        }

        return await dispatcher.SendAsync(message, cancellationToken);
    }
}
