using MediatR;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Features.Monitoring.Commands;

public record RecordResourceSampleCommand(
    Guid DeviceId,
    double? CpuPercent,
    double? MemoryPercent,
    DateTime SampledAt) : IRequest;

public class RecordResourceSampleCommandHandler : IRequestHandler<RecordResourceSampleCommand>
{
    private readonly IMonitoringRepository _monitoringRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public RecordResourceSampleCommandHandler(
        IMonitoringRepository monitoringRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _monitoringRepository = monitoringRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(RecordResourceSampleCommand request, CancellationToken cancellationToken)
    {
        var sample = new ResourceSample
        {
            DeviceId = request.DeviceId,
            CpuPercent = request.CpuPercent,
            MemoryPercent = request.MemoryPercent,
            SampledAt = request.SampledAt
        };

        await _monitoringRepository.AddResourceSampleAsync(sample, cancellationToken);
        await _monitoringRepository.SaveChangesAsync(cancellationToken);

        // Threshold comparison against alert_rules happens in the Worker's
        // AlertThresholdEngine, which calls AlertEvaluationService directly —
        // this handler's job is only to persist the sample and let live
        // dashboards know a fresh number is available.
        await _realtimeNotifier.NotifyResourceSampleReceivedAsync(
            new ResourceSampleReceivedNotification(request.DeviceId, request.CpuPercent, request.MemoryPercent, request.SampledAt),
            cancellationToken);
    }
}
