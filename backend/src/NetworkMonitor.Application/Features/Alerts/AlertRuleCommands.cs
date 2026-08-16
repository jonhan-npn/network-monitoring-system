using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Features.Alerts;

public record CreateAlertRuleCommand(
    Guid? DeviceId,
    string Name,
    string Metric,
    double? ThresholdPercent,
    int ConsecutiveBreaches,
    AlertChannel Channel,
    string Destination) : IRequest<AlertRuleDto>;

public record UpdateAlertRuleCommand(
    Guid Id,
    Guid? DeviceId,
    string Name,
    string Metric,
    double? ThresholdPercent,
    int ConsecutiveBreaches,
    AlertChannel Channel,
    string Destination,
    bool IsEnabled) : IRequest<AlertRuleDto>;

public record DeleteAlertRuleCommand(Guid Id) : IRequest;

public class CreateAlertRuleCommandHandler : IRequestHandler<CreateAlertRuleCommand, AlertRuleDto>
{
    private readonly IAlertRuleRepository _alertRuleRepository;

    public CreateAlertRuleCommandHandler(IAlertRuleRepository alertRuleRepository)
    {
        _alertRuleRepository = alertRuleRepository;
    }

    public async Task<AlertRuleDto> Handle(CreateAlertRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            Name = request.Name,
            Metric = request.Metric,
            ThresholdPercent = request.ThresholdPercent,
            ConsecutiveBreaches = request.ConsecutiveBreaches,
            Channel = request.Channel,
            Destination = request.Destination,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        await _alertRuleRepository.AddAsync(rule, cancellationToken);
        await _alertRuleRepository.SaveChangesAsync(cancellationToken);

        return rule.ToDto();
    }
}

public class UpdateAlertRuleCommandHandler : IRequestHandler<UpdateAlertRuleCommand, AlertRuleDto>
{
    private readonly IAlertRuleRepository _alertRuleRepository;

    public UpdateAlertRuleCommandHandler(IAlertRuleRepository alertRuleRepository)
    {
        _alertRuleRepository = alertRuleRepository;
    }

    public async Task<AlertRuleDto> Handle(UpdateAlertRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _alertRuleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AlertRule), request.Id);

        rule.DeviceId = request.DeviceId;
        rule.Name = request.Name;
        rule.Metric = request.Metric;
        rule.ThresholdPercent = request.ThresholdPercent;
        rule.ConsecutiveBreaches = request.ConsecutiveBreaches;
        rule.Channel = request.Channel;
        rule.Destination = request.Destination;
        rule.IsEnabled = request.IsEnabled;

        _alertRuleRepository.Update(rule);
        await _alertRuleRepository.SaveChangesAsync(cancellationToken);

        return rule.ToDto();
    }
}

public class DeleteAlertRuleCommandHandler : IRequestHandler<DeleteAlertRuleCommand>
{
    private readonly IAlertRuleRepository _alertRuleRepository;

    public DeleteAlertRuleCommandHandler(IAlertRuleRepository alertRuleRepository)
    {
        _alertRuleRepository = alertRuleRepository;
    }

    public async Task Handle(DeleteAlertRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _alertRuleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AlertRule), request.Id);

        _alertRuleRepository.Remove(rule);
        await _alertRuleRepository.SaveChangesAsync(cancellationToken);
    }
}

internal static class AlertRuleMappingExtensions
{
    public static AlertRuleDto ToDto(this AlertRule rule) => new()
    {
        Id = rule.Id,
        DeviceId = rule.DeviceId,
        DeviceName = rule.Device?.Name,
        Name = rule.Name,
        Metric = rule.Metric,
        ThresholdPercent = rule.ThresholdPercent,
        ConsecutiveBreaches = rule.ConsecutiveBreaches,
        Channel = rule.Channel.ToString(),
        Destination = rule.Destination,
        IsEnabled = rule.IsEnabled,
        LastTriggeredAt = rule.LastTriggeredAt
    };
}
