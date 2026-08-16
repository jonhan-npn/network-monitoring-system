using FluentValidation;
using MediatR;
using NetworkMonitor.Application.Common.Exceptions;

namespace NetworkMonitor.Application.Common.Behaviors;

/// <summary>
/// Runs all registered validators for TRequest before the handler executes.
/// Throws ValidationException (caught by the API's ExceptionHandlingMiddleware
/// and translated to a 400) rather than letting invalid data reach a handler.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new AppValidationException(failures);
        }

        return await next();
    }
}
