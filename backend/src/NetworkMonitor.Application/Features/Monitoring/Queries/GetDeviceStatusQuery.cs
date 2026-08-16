using MediatR;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Monitoring.Queries;

public record GetDeviceStatusQuery : IRequest<IReadOnlyList<DeviceStatusDto>>;

public class GetDeviceStatusQueryHandler : IRequestHandler<GetDeviceStatusQuery, IReadOnlyList<DeviceStatusDto>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMonitoringRepository _monitoringRepository;

    public GetDeviceStatusQueryHandler(IDeviceRepository deviceRepository, IMonitoringRepository monitoringRepository)
    {
        _deviceRepository = deviceRepository;
        _monitoringRepository = monitoringRepository;
    }

    public async Task<IReadOnlyList<DeviceStatusDto>> Handle(GetDeviceStatusQuery request, CancellationToken cancellationToken)
    {
        var devices = await _deviceRepository.GetAllMonitoredAsync(cancellationToken);
        var deviceIds = devices.Select(d => d.Id).ToList();
        var latestPings = await _monitoringRepository.GetLatestPingResultsAsync(deviceIds, cancellationToken);

        return devices
            .Select(device =>
            {
                latestPings.TryGetValue(device.Id, out var ping);
                return new DeviceStatusDto
                {
                    DeviceId = device.Id,
                    DeviceName = device.Name,
                    IsOnline = ping?.IsOnline ?? false,
                    LatencyMs = ping?.LatencyMs,
                    CheckedAt = ping?.CheckedAt ?? default
                };
            })
            .ToList();
    }
}
