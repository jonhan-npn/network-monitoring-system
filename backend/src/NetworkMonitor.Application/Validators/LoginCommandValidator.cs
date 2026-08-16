using FluentValidation;
using NetworkMonitor.Application.Features.Authentication;

namespace NetworkMonitor.Application.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty();
    }
}
