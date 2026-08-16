using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Interfaces;

public record AlertMessage(
    Guid RuleId,
    Guid? DeviceId,
    string? DeviceName,
    string Subject,
    string Body,
    AlertChannel Channel,
    string Destination);

/// <summary>
/// Abstraction over "send this alert somewhere." One implementation per
/// channel (EmailNotifier, TelegramNotifier in
/// NetworkMonitor.Infrastructure.Notifications); a composite dispatcher
/// picks the right one based on AlertMessage.Channel so the
/// AlertThresholdEngine in the Worker never needs an if/switch on channel type.
/// </summary>
public interface IAlertDispatcher
{
    AlertChannel Channel { get; }

    Task<bool> SendAsync(AlertMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves the correct IAlertDispatcher for a given channel and sends
/// through it. Implemented in Infrastructure by fanning out to the
/// registered per-channel dispatchers (IEnumerable&lt;IAlertDispatcher&gt;).
/// </summary>
public interface IAlertDispatcherResolver
{
    Task<bool> DispatchAsync(AlertMessage message, CancellationToken cancellationToken = default);
}
