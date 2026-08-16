using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Application.Common.Behaviors;
using NetworkMonitor.Application.Services;

namespace NetworkMonitor.Application;

/// <summary>
/// Wires up everything the Application layer needs into the DI container.
/// Called once from NetworkMonitor.API and NetworkMonitor.Worker's Program.cs
/// (both processes host the same use cases, just triggered differently —
/// HTTP requests vs. background timers).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Runs every FluentValidation validator registered for a request
        // before its handler executes. Keeps handlers free of manual
        // "if (!ModelState.IsValid)"-style boilerplate.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton<DeviceStatusEvaluator>();
        services.AddSingleton<AlertEvaluationService>();

        return services;
    }
}
