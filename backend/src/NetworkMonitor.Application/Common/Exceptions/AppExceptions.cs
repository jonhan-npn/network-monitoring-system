using FluentValidation.Results;

namespace NetworkMonitor.Application.Common.Exceptions;

/// <summary>Maps to 400 Bad Request. Carries one or more field-level failures.</summary>
public class AppValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public AppValidationException()
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public AppValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

/// <summary>Maps to 404 Not Found.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}

/// <summary>Maps to 409 Conflict — e.g. duplicate IP address, duplicate username.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>Maps to 401 Unauthorized — bad credentials, expired/invalid token.</summary>
public class AppAuthenticationException : Exception
{
    public AppAuthenticationException(string message) : base(message)
    {
    }
}

/// <summary>Maps to 403 Forbidden — authenticated, but role doesn't allow the action.</summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You do not have permission to perform this action.")
        : base(message)
    {
    }
}
