using System.Net;
using System.Text.Json;
using NetworkMonitor.Shared.Exceptions;

namespace NetworkMonitor.API.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them into a consistent
/// ProblemDetails-style JSON response, so the Angular client always
/// gets a predictable error shape.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new
        {
            title,
            status = (int)statusCode,
            detail = exception.Message,
            traceId = context.TraceIdentifier,
            // Only leak stack traces in Development to avoid disclosing internals in prod.
            stackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
