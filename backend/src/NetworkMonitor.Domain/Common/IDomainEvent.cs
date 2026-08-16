namespace NetworkMonitor.Domain.Common;

/// <summary>
/// Marker for domain events (see Events/). Not wired into a dispatcher yet —
/// today the Worker raises status transitions inline (RecordPingResultCommand
/// checks StatusChanged directly). These types exist so that if/when you
/// want a real domain-event pipeline (e.g. MediatR INotification published
/// from within a command handler, fanned out to the alert engine, the event
/// log writer, and the SignalR notifier as separate handlers instead of one
/// handler doing all three) the event shapes are already defined and you're
/// refactoring dispatch, not inventing new events.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
