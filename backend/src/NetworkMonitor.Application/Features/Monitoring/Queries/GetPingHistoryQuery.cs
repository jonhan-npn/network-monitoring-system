using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Monitoring.Queries;

public record GetPingHistoryQuery(Guid DeviceId, DateTime From, DateTime To) : IRequest<IReadOnlyList<PingHistoryPointDto>>;

public class GetPingHistoryQueryHandler : IRequestHandler<GetPingHistoryQuery, IReadOnlyList<PingHistoryPointDto>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMonitoringRepository _monitoringRepository;

    public GetPingHistoryQueryHandler(IDeviceRepository deviceRepository, IMonitoringRepository monitoringRepository)
    {
        _deviceRepository = deviceRepository;
        _monitoringRepository = monitoringRepository;
    }

    public async Task<IReadOnlyList<PingHistoryPointDto>> Handle(GetPingHistoryQuery request, CancellationToken cancellationToken)
    {
        _ = await _deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Device), request.DeviceId);

        var results = await _monitoringRepository.GetPingHistoryAsync(
            request.DeviceId, request.From, request.To, cancellationToken);

        return results
            .Select(r => new PingHistoryPointDto
            {
                IsOnline = r.IsOnline,
                LatencyMs = r.LatencyMs,
                CheckedAt = r.CheckedAt
            })
            .ToList();
    }
}
