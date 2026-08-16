using System.Text.Json;
using MediatR;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Application.Features.Alerts;

public record GetAlertRulesQuery : IRequest<IReadOnlyList<AlertRuleDto>>;

public record GetAlertHistoryQuery(int Page, int PageSize) : IRequest<Common.Models.PagedResult<AlertHistoryItemDto>>;

public class GetAlertRulesQueryHandler : IRequestHandler<GetAlertRulesQuery, IReadOnlyList<AlertRuleDto>>
{
    private readonly IAlertRuleRepository _alertRuleRepository;

    public GetAlertRulesQueryHandler(IAlertRuleRepository alertRuleRepository)
    {
        _alertRuleRepository = alertRuleRepository;
    }

    public async Task<IReadOnlyList<AlertRuleDto>> Handle(GetAlertRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await _alertRuleRepository.GetAllAsync(cancellationToken);
        return rules.Select(r => r.ToDto()).ToList();
    }
}

/// <summary>
/// Alert history reads from event_log_entries where EventType == AlertSent
/// (see docs/api.md) rather than a separate table — sent alerts are just
/// one more kind of audited event.
/// </summary>
public class GetAlertHistoryQueryHandler : IRequestHandler<GetAlertHistoryQuery, Common.Models.PagedResult<AlertHistoryItemDto>>
{
    private readonly IEventLogRepository _eventLogRepository;

    public GetAlertHistoryQueryHandler(IEventLogRepository eventLogRepository)
    {
        _eventLogRepository = eventLogRepository;
    }

    public async Task<Common.Models.PagedResult<AlertHistoryItemDto>> Handle(
        GetAlertHistoryQuery request, CancellationToken cancellationToken)
    {
        var (entries, totalCount) = await _eventLogRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            deviceId: null,
            eventType: Domain.Enums.EventType.AlertSent,
            from: null,
            to: null,
            cancellationToken);

        // Metadata (ruleId, ruleName, channel) is stored as a raw JSON
        // string on the event_log_entries.metadata column when
        // AlertThresholdEngine logs the AlertSent event — parsed
        // defensively here rather than duplicating the data into its own
        // table or over-committing to a strict schema for it.
        var items = entries
            .Select(e =>
            {
                var (ruleId, ruleName, channel) = ParseAlertMetadata(e.Metadata);
                return new AlertHistoryItemDto
                {
                    Id = e.Id,
                    RuleId = ruleId,
                    RuleName = ruleName,
                    DeviceId = e.DeviceId,
                    DeviceName = e.Device?.Name,
                    Message = e.Message,
                    Channel = channel,
                    TriggeredAt = e.OccurredAt
                };
            })
            .ToList();

        return Common.Models.PagedResult<AlertHistoryItemDto>.Create(items, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// Best-effort parse of the AlertSent event's JSON metadata, written by
    /// AlertThresholdEngine as: <c>{ "ruleId": "...", "ruleName": "...", "channel": "Email" }</c>.
    /// Never throws — a malformed/missing metadata blob shouldn't break the
    /// whole history page, it just falls back to empty values for that row.
    /// </summary>
    private static (Guid RuleId, string RuleName, string Channel) ParseAlertMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return (Guid.Empty, string.Empty, string.Empty);
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;

            var ruleId = root.TryGetProperty("ruleId", out var ruleIdProp) && ruleIdProp.TryGetGuid(out var parsedId)
                ? parsedId
                : Guid.Empty;

            var ruleName = root.TryGetProperty("ruleName", out var ruleNameProp)
                ? ruleNameProp.GetString() ?? string.Empty
                : string.Empty;

            var channel = root.TryGetProperty("channel", out var channelProp)
                ? channelProp.GetString() ?? string.Empty
                : string.Empty;

            return (ruleId, ruleName, channel);
        }
        catch (JsonException)
        {
            return (Guid.Empty, string.Empty, string.Empty);
        }
    }
}
