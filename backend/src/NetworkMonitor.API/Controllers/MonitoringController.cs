using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.Common.Models;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Dashboard;
using NetworkMonitor.Application.Features.Monitoring.Queries;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.API.Controllers;

[ApiController]
[Authorize]
[Route("api/monitoring")]
public class MonitoringController(ISender sender) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<IReadOnlyList<DeviceStatusDto>>> GetCurrentStatus(CancellationToken ct) =>
        Ok(await sender.Send(new GetDeviceStatusQuery(), ct));

    [HttpGet("devices/{deviceId:guid}/ping-history")]
    public async Task<ActionResult<IReadOnlyList<PingHistoryPointDto>>> GetPingHistory(Guid deviceId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (start, end) = GetRange(from, to);
        return Ok(await sender.Send(new GetPingHistoryQuery(deviceId, start, end), ct));
    }

    [HttpGet("devices/{deviceId:guid}/resource-history")]
    public async Task<ActionResult<IReadOnlyList<ResourceHistoryPointDto>>> GetResourceHistory(Guid deviceId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (start, end) = GetRange(from, to);
        return Ok(await sender.Send(new GetResourceHistoryQuery(deviceId, start, end), ct));
    }

    [HttpGet("dashboard/summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await sender.Send(new GetDashboardSummaryQuery(), ct));

    [HttpGet("events")]
    public async Task<ActionResult<PagedResult<EventLogDto>>> GetEventLog([FromQuery] Guid? deviceId,
        [FromQuery] string? eventType, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (!TryParseEventType(eventType, out var parsedEventType)) return ValidationProblem("EventType is not valid.");
        return Ok(await sender.Send(new GetEventLogQuery(Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), deviceId,
            parsedEventType, from, to), ct));
    }

    private static (DateTime Start, DateTime End) GetRange(DateTime? from, DateTime? to)
    {
        var end = to?.ToUniversalTime() ?? DateTime.UtcNow;
        return (from?.ToUniversalTime() ?? end.AddDays(-7), end);
    }

    private static bool TryParseEventType(string? eventType, out EventType? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(eventType)) return true;
        if (!Enum.TryParse<EventType>(eventType, true, out var parsed) || !Enum.IsDefined(parsed)) return false;
        value = parsed;
        return true;
    }
}
