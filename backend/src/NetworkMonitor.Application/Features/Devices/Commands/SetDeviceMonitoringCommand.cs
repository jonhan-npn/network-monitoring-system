using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Devices.Commands;

public record SetDeviceMonitoringCommand(Guid Id, bool IsMonitored) : IRequest;

public class SetDeviceMonitoringCommandHandler : IRequestHandler<SetDeviceMonitoringCommand>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDateTimeProvider _clock;

    public SetDeviceMonitoringCommandHandler(IDeviceRepository deviceRepository, IDateTimeProvider clock)
    {
        _deviceRepository = deviceRepository;
        _clock = clock;
    }

    public async Task Handle(SetDeviceMonitoringCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Device), request.Id);

        // Toggling this off is how an operator pauses polling for a device
        // under planned maintenance without deleting it (and losing history).
        device.IsMonitored = request.IsMonitored;
        device.UpdatedAt = _clock.UtcNow;

        _deviceRepository.Update(device);
        await _deviceRepository.SaveChangesAsync(cancellationToken);
    }
}
