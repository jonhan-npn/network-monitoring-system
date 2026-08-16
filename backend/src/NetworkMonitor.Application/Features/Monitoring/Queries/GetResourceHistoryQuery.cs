using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Monitoring.Queries;

public record GetResourceHistoryQuery(Guid DeviceId, DateTime From, DateTime To) : IRequest<IReadOnlyList<ResourceHistoryPointDto>>;

public class GetResourceHistoryQueryHandler : IRequestHandler<GetResourceHistoryQuery, IReadOnlyList<ResourceHistoryPointDto>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMonitoringRepository _monitoringRepository;

    public GetResourceHistoryQueryHandler(IDeviceRepository deviceRepository, IMonitoringRepository monitoringRepository)
    {
        _deviceRepository = deviceRepository;
        _monitoringRepository = monitoringRepository;
    }

    public async Task<IReadOnlyList<ResourceHistoryPointDto>> Handle(GetResourceHistoryQuery request, CancellationToken cancellationToken)
    {
        _ = await _deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Device), request.DeviceId);

        var samples = await _monitoringRepository.GetResourceHistoryAsync(
            request.DeviceId, request.From, request.To, cancellationToken);

        return samples
            .Select(s => new ResourceHistoryPointDto
            {
                CpuPercent = s.CpuPercent,
                MemoryPercent = s.MemoryPercent,
                SampledAt = s.SampledAt
            })
            .ToList();
    }
}
