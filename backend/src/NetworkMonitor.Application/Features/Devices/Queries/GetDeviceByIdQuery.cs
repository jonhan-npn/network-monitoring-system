using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Devices;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Devices.Queries;

public record GetDeviceByIdQuery(Guid Id) : IRequest<DeviceDto>;

public class GetDeviceByIdQueryHandler : IRequestHandler<GetDeviceByIdQuery, DeviceDto>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMonitoringRepository _monitoringRepository;

    public GetDeviceByIdQueryHandler(IDeviceRepository deviceRepository, IMonitoringRepository monitoringRepository)
    {
        _deviceRepository = deviceRepository;
        _monitoringRepository = monitoringRepository;
    }

    public async Task<DeviceDto> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Device), request.Id);

        var latestPing = await _monitoringRepository.GetLatestPingResultAsync(device.Id, cancellationToken);
        var latestSample = await _monitoringRepository.GetLatestResourceSampleAsync(device.Id, cancellationToken);

        return device.ToDto(
            isOnline: latestPing?.IsOnline,
            lastCheckedAt: latestPing?.CheckedAt,
            lastCpuPercent: latestSample?.CpuPercent,
            lastMemoryPercent: latestSample?.MemoryPercent);
    }
}
