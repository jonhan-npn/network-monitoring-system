using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NetworkMonitor.API.Configuration;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Infrastructure.Authentication;
using NetworkMonitor.Infrastructure.Data;
using NetworkMonitor.Infrastructure.Notifications;
using NetworkMonitor.Infrastructure.Repositories;

namespace NetworkMonitor.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core (PostgreSQL), repositories, domain services and options.
    /// Keeps Program.cs thin.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MonitoringOptions>(configuration.GetSection(MonitoringOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly("NetworkMonitor.Infrastructure")));

        // Repositories
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Auth helpers
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Notifications (used by AlertsController for test-sends; the real
        // dispatch loop lives in the Worker)
        services.AddScoped<IAlertDispatcher, CompositeAlertDispatcher>();
        services.AddScoped<EmailNotifier>();
        services.AddScoped<TelegramNotifier>();

        services.AddSignalR();

        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:4200" };

                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // required for SignalR
            });
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Enterprise Network Monitor API",
                Version = "v1",
                Description = "REST API for device inventory, ping/SNMP monitoring, alerts and event logs."
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Scheme = "bearer",
                BearerFormat = "JWT",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Description = "Enter the access token returned from /api/auth/login."
            };

            options.AddSecurityDefinition("Bearer", jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
            });
        });

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        return services;
    }
}
