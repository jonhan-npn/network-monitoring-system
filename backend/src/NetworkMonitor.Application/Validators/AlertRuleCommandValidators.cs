using FluentValidation;
using NetworkMonitor.Application.Features.Alerts;

namespace NetworkMonitor.Application.Validators;

file static class AlertRuleValidationRules
{
    public static readonly string[] ValidMetrics = ["Availability", "Cpu", "Memory"];
}

public class CreateAlertRuleCommandValidator : AbstractValidator<CreateAlertRuleCommand>
{
    public CreateAlertRuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);

        RuleFor(x => x.Metric)
            .Must(m => AlertRuleValidationRules.ValidMetrics.Contains(m))
            .WithMessage($"Metric must be one of: {string.Join(", ", AlertRuleValidationRules.ValidMetrics)}.");

        // Threshold is required for Cpu/Memory rules, ignored for Availability
        // (which fires on consecutive offline pings instead — see docs/database.md).
        RuleFor(x => x.ThresholdPercent)
            .NotNull()
            .InclusiveBetween(0, 100)
            .When(x => x.Metric is "Cpu" or "Memory")
            .WithMessage("Threshold percent (0–100) is required for Cpu/Memory rules.");

        RuleFor(x => x.ConsecutiveBreaches)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.Channel).IsInEnum();

        RuleFor(x => x.Destination)
            .NotEmpty()
            .MaximumLength(256);
    }
}

public class UpdateAlertRuleCommandValidator : AbstractValidator<UpdateAlertRuleCommand>
{
    public UpdateAlertRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);

        RuleFor(x => x.Metric)
            .Must(m => AlertRuleValidationRules.ValidMetrics.Contains(m))
            .WithMessage($"Metric must be one of: {string.Join(", ", AlertRuleValidationRules.ValidMetrics)}.");

        RuleFor(x => x.ThresholdPercent)
            .NotNull()
            .InclusiveBetween(0, 100)
            .When(x => x.Metric is "Cpu" or "Memory")
            .WithMessage("Threshold percent (0–100) is required for Cpu/Memory rules.");

        RuleFor(x => x.ConsecutiveBreaches)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.Channel).IsInEnum();

        RuleFor(x => x.Destination)
            .NotEmpty()
            .MaximumLength(256);
    }
}
