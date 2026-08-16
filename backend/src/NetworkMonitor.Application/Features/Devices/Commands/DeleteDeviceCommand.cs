using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Devices.Commands;

public record DeleteDeviceCommand(Guid Id) : IRequest;

public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand>
{
    private readonly IDeviceRepository _deviceRepository;

    public DeleteDeviceCommandHandler(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Device), request.Id);

        // Ping/resource history cascades via the FK's ON DELETE CASCADE
        // (see infrastructure/postgres/init.sql); event log entries are
        // preserved with device_id set to NULL (ON DELETE SET NULL) so the
        // audit trail survives the device being removed.
        _deviceRepository.Remove(device);
        await _deviceRepository.SaveChangesAsync(cancellationToken);
    }
}
