using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Infrastructure.Logging;

namespace NetworkMonitor.Worker.Jobs;

/// <summary>
/// Implements retention option 3 from docs/database.md: a scheduled
/// `DELETE ... WHERE checked_at &lt; now() - retention` sweep. Deliberately
/// the simplest of the three options discussed there (vs. partitioning or
/// downsampling) — appropriate for a project still working through the
/// build order's steps 6–9. Revisit once you can see real row-volume
/// numbers in production, per that doc's closing note.
/// </summary>
public class DataRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringWorkerOptions _options;
    private readonly ILogger<DataRetentionJob> _logger;

    public DataRetentionJob(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringWorkerOptions> options,
        ILogger<DataRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.RetentionCheckIntervalHours));

        // No immediate run on startup — retention is a housekeeping task,
        // not something that needs to happen the second the Worker boots,
        // and running it on every container restart during active
        // development would be needless churn.
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunPurgeAsync(stoppingToken);
        }
    }

    private async Task RunPurgeAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var monitoringRepository = scope.ServiceProvider.GetRequiredService<IMonitoringRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var cutoff = clock.UtcNow.AddDays(-_options.RetentionDays);

        try
        {
            var purgedPings = await monitoringRepository.PurgeOldPingResultsAsync(cutoff, cancellationToken);
            var purgedSamples = await monitoringRepository.PurgeOldResourceSamplesAsync(cutoff, cancellationToken);

            _logger.LogInformation(
                LogEvents.RetentionPurgeCompleted,
                "Retention purge removed {PingCount} ping_results and {SampleCount} resource_samples older than {Cutoff:u}",
                purgedPings, purgedSamples, cutoff);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Retention purge failed unexpectedly");
        }
    }
}
