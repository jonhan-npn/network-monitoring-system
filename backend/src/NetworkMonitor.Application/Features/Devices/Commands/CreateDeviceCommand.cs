using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Devices;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Features.Devices.Commands;

public record CreateDeviceCommand(
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

public class CreateDeviceCommandHandler : IRequestHandler<CreateDeviceCommand, DeviceDto>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDateTimeProvider _clock;

    public CreateDeviceCommandHandler(IDeviceRepository deviceRepository, IDateTimeProvider clock)
    {
        _deviceRepository = deviceRepository;
        _clock = clock;
    }

    public async Task<DeviceDto> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        if (await _deviceRepository.ExistsWithIpAsync(request.IpAddress, cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                $"A device with IP address '{request.IpAddress}' is already being monitored.");
        }

        var now = _clock.UtcNow;

        var device = new Device
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IpAddress = request.IpAddress,
            DeviceType = request.DeviceType,
            Location = request.Location,
            Description = request.Description,
            SnmpCommunity = request.SnmpCommunity,
            SnmpPort = request.SnmpPort,
            SnmpVersion = request.SnmpVersion,
            SshPort = request.SshPort,
            SshUsername = request.SshUsername,
            SshCredentialRef = request.SshCredentialRef,
            IsMonitored = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _deviceRepository.AddAsync(device, cancellationToken);
        await _deviceRepository.SaveChangesAsync(cancellationToken);

        return device.ToDto();
    }
}
