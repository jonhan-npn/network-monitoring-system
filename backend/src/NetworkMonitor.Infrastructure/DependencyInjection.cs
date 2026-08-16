using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Infrastructure.Authentication;
using NetworkMonitor.Infrastructure.Common;
using NetworkMonitor.Infrastructure.Data;
using NetworkMonitor.Infrastructure.Monitoring;
using NetworkMonitor.Infrastructure.Notifications;
using NetworkMonitor.Infrastructure.Ping;
using NetworkMonitor.Infrastructure.Repositories;
using NetworkMonitor.Infrastructure.SNMP;
using NetworkMonitor.Infrastructure.SSH;

namespace NetworkMonitor.Infrastructure;

/// <summary>
/// Called from both NetworkMonitor.API and NetworkMonitor.Worker's
/// Program.cs — everything here is shared. API-only concerns (like the
/// concrete ICurrentUserService that reads IHttpContextAccessor) are NOT
/// registered here; they're registered in the API project itself, since
/// Infrastructure has no ASP.NET Core hosting dependency and shouldn't
/// gain one just for that.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- Data access ----
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IEventLogRepository, EventLogRepository>();
        services.AddScoped<IMonitoringRepository, MonitoringRepository>();

        // ---- Cross-cutting ----
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // ---- Auth ----
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // ---- Alert dispatch ----
        services.Configure<Notifications.SmtpSettings>(configuration.GetSection(Notifications.SmtpSettings.SectionName));
        services.Configure<Notifications.TelegramSettings>(configuration.GetSection(Notifications.TelegramSettings.SectionName));

        services.AddScoped<IAlertDispatcher, EmailNotifier>();
        services.AddHttpClient<IAlertDispatcher, TelegramNotifier>(); // typed HttpClient via IHttpClientFactory — avoids socket exhaustion under load
        services.AddScoped<IAlertDispatcherResolver, AlertDispatcherResolver>();

        // ---- Realtime (Worker -> API bridge; see HttpRealtimeNotifier) ----
        services.Configure<RealtimeNotifierSettings>(configuration.GetSection(RealtimeNotifierSettings.SectionName));
        services.AddHttpClient<IRealtimeNotifier, HttpRealtimeNotifier>();

        // ---- Ping / SNMP / SSH ----
        services.AddSingleton<IPingService, PingSweepService>();
        services.AddSingleton<ISnmpService, SnmpPollingService>();
        services.AddSingleton<SSH.ISecretResolver, SSH.EnvironmentVariableSecretResolver>();
        services.AddScoped<ISshService, SshInventoryService>();

        return services;
    }
}
