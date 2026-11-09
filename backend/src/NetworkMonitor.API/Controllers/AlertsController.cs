using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.Common.Models;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Alerts;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.API.Controllers;

/// <summary>
/// Alert rule CRUD plus the sent-alert history screen. Follows the same
/// CQRS-through-MediatR pattern as DevicesController/MonitoringController:
/// controllers only translate HTTP into a command/query and back — all of
/// the actual work lives in NetworkMonitor.Application.Features.Alerts.
/// </summary>
[ApiController]
[Authorize]
[Route("api/alerts")]
public class AlertsController(ISender sender, IAlertDispatcherResolver dispatcherResolver) : ControllerBase
{
    /// <summary>Lists configured alert rules (e.g. "CPU &gt; 90%", "device offline for 3 pings").</summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRuleDto>>> GetRules(CancellationToken ct) =>
        Ok(await sender.Send(new GetAlertRulesQuery(), ct));

    [HttpPost("rules")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(AlertRuleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AlertRuleDto>> CreateRule([FromBody] CreateAlertRuleRequest request, CancellationToken ct)
    {
        if (!TryParseChannel(request.Channel, out var channel))
        {
            return ValidationProblem("Channel must be 'Email' or 'Telegram'.");
        }

        var created = await sender.Send(
            new CreateAlertRuleCommand(
                request.DeviceId,
                request.Name,
                request.Metric,
                request.ThresholdPercent,
                request.ConsecutiveBreaches,
                channel,
                request.Destination),
            ct);

        return CreatedAtAction(nameof(GetRules), null, created);
    }

    [HttpPut("rules/{id:guid}")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(AlertRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertRuleDto>> UpdateRule(Guid id, [FromBody] UpdateAlertRuleRequest request, CancellationToken ct)
    {
        if (!TryParseChannel(request.Channel, out var channel))
        {
            return ValidationProblem("Channel must be 'Email' or 'Telegram'.");
        }

        var updated = await sender.Send(
            new UpdateAlertRuleCommand(
                id,
                request.DeviceId,
                request.Name,
                request.Metric,
                request.ThresholdPercent,
                request.ConsecutiveBreaches,
                channel,
                request.Destination,
                request.IsEnabled),
            ct);

        return Ok(updated);
    }

    [HttpDelete("rules/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteAlertRuleCommand(id), ct);
        return NoContent();
    }

    /// <summary>Paged history of alerts that have actually fired, for the "Alert History" screen.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<AlertHistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AlertHistoryItemDto>>> GetHistory(
        [FromQuery] Guid? deviceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(page, 1);

        return Ok(await sender.Send(new GetAlertHistoryQuery(page, pageSize, deviceId), ct));
    }

    /// <summary>
    /// Sends a test alert through the requested channel, so users can confirm
    /// their Telegram/email setup works before relying on it. Requires a
    /// destination (email address or Telegram chat id) because no rule is
    /// being evaluated here to supply one.
    /// </summary>
    [HttpPost("test")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendTestAlert([FromBody] TestAlertRequest request, CancellationToken ct)
    {
        if (!TryParseChannel(request.Channel, out var channel))
        {
            return ValidationProblem("Channel must be 'Email' or 'Telegram'.");
        }

        var message = new AlertMessage(
            RuleId: Guid.Empty,
            DeviceId: null,
            DeviceName: null,
            Subject: "Network Monitor test alert",
            Body: "This is a test alert triggered from the dashboard. If you received this, the channel is configured correctly.",
            Channel: channel,
            Destination: request.Destination);

        var sent = await dispatcherResolver.DispatchAsync(message, ct);

        return sent
            ? Accepted()
            : StatusCode(StatusCodes.Status502BadGateway, "The alert could not be delivered. Check the channel configuration and Worker logs.");
    }

    private static bool TryParseChannel(string? channel, out AlertChannel value)
    {
        value = default;
        return !string.IsNullOrWhiteSpace(channel)
            && Enum.TryParse(channel, true, out value)
            && Enum.IsDefined(value);
    }
}

public record CreateAlertRuleRequest(
    string Name,
    Guid? DeviceId,
    string Metric,
    double? ThresholdPercent,
    int ConsecutiveBreaches,
    string Channel,
    string Destination);

public record UpdateAlertRuleRequest(
    string Name,
    Guid? DeviceId,
    string Metric,
    double? ThresholdPercent,
    int ConsecutiveBreaches,
    string Channel,
    string Destination,
    bool IsEnabled);

public record TestAlertRequest(string Channel, string Destination);
