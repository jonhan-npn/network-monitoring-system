using FluentValidation;
using NetworkMonitor.Application.Features.Users;

namespace NetworkMonitor.Application.Validators;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "Operator", "Viewer"];

    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        // Matches the minimum enforced by the seed admin account's
        // "ChangeMe123!" in infrastructure/postgres/init.sql — 8+ chars,
        // at least one letter and one digit. Tune to your actual policy.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Role)
            .Must(role => ValidRoles.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", ValidRoles)}.");
    }
}
