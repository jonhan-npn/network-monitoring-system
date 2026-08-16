using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Devices;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Features.Devices.Commands;

public record UpdateDeviceCommand(
    Guid Id,
    string Name,
    string IpAddress,
    DeviceType DeviceType,
    string? Location,
    string? Description,
    string SnmpCommunity,
    int SnmpPort,
    string SnmpVersion,
    int SshPort,
    string? SshUsername,
    string? SshCredentialRef) : IRequest<DeviceDto>;

public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, DeviceDto>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDateTimeProvider _clock;

    public UpdateDeviceCommandHandler(IDeviceRepository deviceRepository, IDateTimeProvider clock)
    {
        _deviceRepository = deviceRepository;
        _clock = clock;
    }

    public async Task<DeviceDto> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Device), request.Id);

        if (await _deviceRepository.ExistsWithIpAsync(request.IpAddress, request.Id, cancellationToken))
        {
            throw new ConflictException(
                $"Another device is already using IP address '{request.IpAddress}'.");
        }

        device.Name = request.Name;
        device.IpAddress = request.IpAddress;
        device.DeviceType = request.DeviceType;
        device.Location = request.Location;
        device.Description = request.Description;
        device.SnmpCommunity = request.SnmpCommunity;
        device.SnmpPort = request.SnmpPort;
        device.SnmpVersion = request.SnmpVersion;
        device.SshPort = request.SshPort;
        device.SshUsername = request.SshUsername;
        device.SshCredentialRef = request.SshCredentialRef;
        device.UpdatedAt = _clock.UtcNow;

        _deviceRepository.Update(device);
        await _deviceRepository.SaveChangesAsync(cancellationToken);

        return device.ToDto();
    }
}
