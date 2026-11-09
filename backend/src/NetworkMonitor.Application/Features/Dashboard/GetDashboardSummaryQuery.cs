using MediatR;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Dashboard;

/// <summary>
/// Backs GET /api/monitoring/dashboard/summary (docs/api.md). Deliberately
/// a single query that fans out to several repositories rather than a
/// dedicated "dashboard repository" — the aggregation logic itself belongs
/// here in the Application layer, not hidden behind a repository method
/// that would just be doing the same joins anyway.
/// </summary>
public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMonitoringRepository _monitoringRepository;
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IDateTimeProvider _clock;

    public GetDashboardSummaryQueryHandler(
        IDeviceRepository deviceRepository,
        IMonitoringRepository monitoringRepository,
        IAlertRuleRepository alertRuleRepository,
        IEventLogRepository eventLogRepository,
        IDateTimeProvider clock)
    {
        _deviceRepository = deviceRepository;
        _monitoringRepository = monitoringRepository;
        _alertRuleRepository = alertRuleRepository;
        _eventLogRepository = eventLogRepository;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var devices = await _deviceRepository.GetAllMonitoredAsync(cancellationToken);
        var deviceIds = devices.Select(d => d.Id).ToList();

        var latestPings = await _monitoringRepository.GetLatestPingResultsAsync(deviceIds, cancellationToken);

        var onlineCount = deviceIds.Count(id => latestPings.TryGetValue(id, out var ping) && ping.IsOnline);
        var totalCount = deviceIds.Count;

        // Average current CPU/RAM across devices that have at least one
        // resource sample yet — devices that only just started SNMP polling
        // (or don't support SNMP at all) are excluded rather than counted
        // as 0%, which would otherwise drag the average down misleadingly.
        // One batched query rather than Task.WhenAll over per-device calls:
        // DbContext is not thread-safe, so the parallel version intermittently
        // threw "a second operation was started on this context instance".
        var latestSamples = await _monitoringRepository
            .GetLatestResourceSamplesAsync(deviceIds, cancellationToken);

        var samplesWithData = latestSamples.Values.ToList();
        var avgCpu = samplesWithData.Count > 0
            ? samplesWithData.Average(s => s!.CpuPercent ?? 0)
            : 0;
        var avgMemory = samplesWithData.Count > 0
            ? samplesWithData.Average(s => s!.MemoryPercent ?? 0)
            : 0;

        var alertRules = await _alertRuleRepository.GetAllAsync(cancellationToken);
        var activeAlertRules = alertRules.Count(r => r.IsEnabled);

        var since = _clock.UtcNow.AddHours(-24);
        var (_, alertsLast24h) = await _eventLogRepository.GetPagedAsync(
            page: 1,
            pageSize: 1, // only need TotalCount here, not the rows themselves
            deviceId: null,
            eventType: Domain.Enums.EventType.AlertSent,
            from: since,
            to: _clock.UtcNow,
            cancellationToken);

        return new DashboardSummaryDto
        {
            TotalDevices = totalCount,
            OnlineDevices = onlineCount,
            OfflineDevices = totalCount - onlineCount,
            AverageCpuPercent = Math.Round(avgCpu, 1),
            AverageMemoryPercent = Math.Round(avgMemory, 1),
            ActiveAlertRules = activeAlertRules,
            AlertsTriggeredLast24h = alertsLast24h
        };
    }
}
