// TODO: Implement AlertsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Alerts;

namespace NetworkMonitor.API.Controllers;

[ApiController]
[Authorize]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly IAlertRuleService _ruleService;
    private readonly IAlertHistoryService _historyService;
    private readonly IAlertDispatcher _alertDispatcher;

    public AlertsController(
        IAlertRuleService ruleService,
        IAlertHistoryService historyService,
        IAlertDispatcher alertDispatcher)
    {
        _ruleService = ruleService;
        _historyService = historyService;
        _alertDispatcher = alertDispatcher;
    }

    /// <summary>Lists configured alert rules (e.g. "CPU > 90% for 5 min", "device offline for 1 min").</summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRuleDto>>> GetRules(CancellationToken ct)
    {
        return Ok(await _ruleService.GetRulesAsync(ct));
    }

    [HttpPost("rules")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(AlertRuleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AlertRuleDto>> CreateRule([FromBody] CreateAlertRuleRequest request, CancellationToken ct)
    {
        var created = await _ruleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetRules), new { }, created);
    }

    [HttpPut("rules/{id:guid}")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(AlertRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertRuleDto>> UpdateRule(Guid id, [FromBody] UpdateAlertRuleRequest request, CancellationToken ct)
    {
        var updated = await _ruleService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("rules/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct)
    {
        var deleted = await _ruleService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Paged history of alerts that have actually fired, for the "Alert History" screen.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<AlertHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AlertHistoryDto>>> GetHistory(
        [FromQuery] Guid? deviceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(page, 1);

        return Ok(await _historyService.GetHistoryAsync(deviceId, page, pageSize, ct));
    }

    /// <summary>Sends a test alert through the configured channel(s), so users can confirm their Telegram/email setup works before relying on it.</summary>
    [HttpPost("test")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SendTestAlert([FromBody] TestAlertRequest request, CancellationToken ct)
    {
        await _alertDispatcher.SendAsync(
            channel: request.Channel,
            subject: "Network Monitor test alert",
            message: "This is a test alert triggered from the dashboard. If you received this, the channel is configured correctly.",
            ct: ct);

        return Accepted();
    }
}

public record CreateAlertRuleRequest(string Name, Guid? DeviceId, string MetricType, string Comparator, double Threshold, int DurationSeconds, string Channel);
public record UpdateAlertRuleRequest(string Name, string Comparator, double Threshold, int DurationSeconds, string Channel, bool IsEnabled);
public record TestAlertRequest(string Channel);
