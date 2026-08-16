using MediatR;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Application.Services;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Features.Monitoring.Commands;

/// <summary>
/// Records one ping sweep result for a device. Returns whether this
/// represents a status transition, so the Worker knows whether to also
/// run the alert threshold engine and push a SignalR update.
/// </summary>
public record RecordPingResultCommand(
    Guid DeviceId,
    string DeviceName,
    bool IsOnline,
    double? LatencyMs,
    DateTime CheckedAt) : IRequest<RecordPingResultResult>;

public record RecordPingResultResult(bool StatusChanged, bool IsOnline);

public class RecordPingResultCommandHandler : IRequestHandler<RecordPingResultCommand, RecordPingResultResult>
{
    private readonly IMonitoringRepository _monitoringRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly DeviceStatusEvaluator _statusEvaluator;

    public RecordPingResultCommandHandler(
        IMonitoringRepository monitoringRepository,
        IEventLogRepository eventLogRepository,
        IRealtimeNotifier realtimeNotifier,
        DeviceStatusEvaluator statusEvaluator)
    {
        _monitoringRepository = monitoringRepository;
        _eventLogRepository = eventLogRepository;
        _realtimeNotifier = realtimeNotifier;
        _statusEvaluator = statusEvaluator;
    }

    public async Task<RecordPingResultResult> Handle(RecordPingResultCommand request, CancellationToken cancellationToken)
    {
        var previous = await _monitoringRepository.GetLatestPingResultAsync(request.DeviceId, cancellationToken);
        var evaluation = _statusEvaluator.Evaluate(previous, request.IsOnline);

        var pingResult = new PingResult
        {
            DeviceId = request.DeviceId,
            IsOnline = request.IsOnline,
            LatencyMs = request.LatencyMs,
            CheckedAt = request.CheckedAt
        };

        await _monitoringRepository.AddPingResultAsync(pingResult, cancellationToken);

        if (evaluation.StatusChanged)
        {
            var eventType = request.IsOnline ? EventType.DeviceOnline : EventType.DeviceOffline;
            var message = request.IsOnline
                ? $"{request.DeviceName} came back online."
                : $"{request.DeviceName} stopped responding to ping.";

            await _eventLogRepository.AddAsync(new EventLogEntry
            {
                DeviceId = request.DeviceId,
                EventType = eventType,
                Message = message,
                OccurredAt = request.CheckedAt
            }, cancellationToken);
        }

        await _monitoringRepository.SaveChangesAsync(cancellationToken);

        // Push regardless of whether status changed, so a live-open dashboard's
        // "last checked" timestamp stays current — but the Worker only needs
        // to run the alert engine when StatusChanged is true.
        await _realtimeNotifier.NotifyDeviceStatusChangedAsync(
            new DeviceStatusChangedNotification(request.DeviceId, request.IsOnline, request.CheckedAt),
            cancellationToken);

        return new RecordPingResultResult(evaluation.StatusChanged, request.IsOnline);
    }
}
