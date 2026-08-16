using MediatR;
using NetworkMonitor.Application.Common.Models;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Devices;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Features.Devices.Queries;

public record GetDevicesQuery(
    int Page,
    int PageSize,
    string? Search,
    DeviceType? DeviceType,
    bool? IsMonitored) : IRequest<PagedResult<DeviceListItemDto>>;

public class GetDevicesQueryHandler : IRequestHandler<GetDevicesQuery, PagedResult<DeviceListItemDto>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMonitoringRepository _monitoringRepository;

    public GetDevicesQueryHandler(IDeviceRepository deviceRepository, IMonitoringRepository monitoringRepository)
    {
        _deviceRepository = deviceRepository;
        _monitoringRepository = monitoringRepository;
    }

    public async Task<PagedResult<DeviceListItemDto>> Handle(GetDevicesQuery request, CancellationToken cancellationToken)
    {
        var (devices, totalCount) = await _deviceRepository.GetPagedAsync(
            request.Page, request.PageSize, request.Search, request.DeviceType, request.IsMonitored, cancellationToken);

        var deviceIds = devices.Select(d => d.Id).ToList();
        var latestPings = await _monitoringRepository.GetLatestPingResultsAsync(deviceIds, cancellationToken);

        var items = devices
            .Select(d =>
            {
                latestPings.TryGetValue(d.Id, out var ping);
                return d.ToListItemDto(ping?.IsOnline, ping?.CheckedAt);
            })
            .ToList();

        return PagedResult<DeviceListItemDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
