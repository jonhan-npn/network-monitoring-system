using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Monitoring.Queries;

namespace NetworkMonitor.API.Controllers;

[ApiController]
[Authorize]
[Route("api/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringQueryService _monitoringQueryService;

    public MonitoringController(IMonitoringQueryService monitoringQueryService)
    {
        _monitoringQueryService = monitoringQueryService;
    }

    /// <summary>Current online/offline status for every device — the main dashboard summary.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeviceStatusDto>>> GetCurrentStatus(CancellationToken ct)
    {
        var statuses = await _monitoringQueryService.GetCurrentStatusesAsync(ct);
        return Ok(statuses);
    }

    /// <summary>Ping history for a device, used to render uptime/latency charts.</summary>
    [HttpGet("devices/{deviceId:guid}/ping-history")]
    [ProducesResponseType(typeof(IReadOnlyList<PingResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PingResultDto>>> GetPingHistory(
        Guid deviceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var history = await _monitoringQueryService.GetPingHistoryAsync(deviceId, from, to, ct);
        return Ok(history);
    }

    /// <summary>CPU/RAM sample history for a device, pulled via SNMP by the worker.</summary>
    [HttpGet("devices/{deviceId:guid}/resource-history")]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceSampleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResourceSampleDto>>> GetResourceHistory(
        Guid deviceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var history = await _monitoringQueryService.GetResourceHistoryAsync(deviceId, from, to, ct);
        return Ok(history);
    }

    /// <summary>Aggregated numbers for the dashboard header (device count, % online, active alerts, etc.).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
    {
        var summary = await _monitoringQueryService.GetDashboardSummaryAsync(ct);
        return Ok(summary);
    }

    /// <summary>Structured event log (online→offline transitions, config changes, etc.), paged and filterable.</summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(PagedResult<EventLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventLogDto>>> GetEventLog(
        [FromQuery] Guid? deviceId,
        [FromQuery] string? eventType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(page, 1);

        var result = await _monitoringQueryService.GetEventLogAsync(deviceId, eventType, page, pageSize, ct);
        return Ok(result);
    }
}
