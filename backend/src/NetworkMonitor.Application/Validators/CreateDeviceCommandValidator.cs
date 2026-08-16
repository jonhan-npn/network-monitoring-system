using System.Net;
using FluentValidation;
using NetworkMonitor.Application.Features.Devices.Commands;

namespace NetworkMonitor.Application.Validators;

public class CreateDeviceCommandValidator : AbstractValidator<CreateDeviceCommand>
{
    public CreateDeviceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .Must(BeAValidIpAddress).WithMessage("'{PropertyName}' must be a valid IPv4 or IPv6 address.");

        RuleFor(x => x.DeviceType)
            .IsInEnum();

        RuleFor(x => x.SnmpPort)
            .InclusiveBetween(1, 65535);

        RuleFor(x => x.SnmpVersion)
            .Must(v => v is "1" or "2c" or "3")
            .WithMessage("SNMP version must be '1', '2c', or '3'.");

        RuleFor(x => x.SshPort)
            .InclusiveBetween(1, 65535);

        RuleFor(x => x.Location)
            .MaximumLength(256);

        RuleFor(x => x.Description)
            .MaximumLength(512);
    }

    private static bool BeAValidIpAddress(string ip) => IPAddress.TryParse(ip, out _);
}
