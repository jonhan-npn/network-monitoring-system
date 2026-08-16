using MediatR;
using NetworkMonitor.Application.Common.Models;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Application.Features.Monitoring.Queries;

public record GetEventLogQuery(
    int Page,
    int PageSize,
    Guid? DeviceId,
    EventType? EventType,
    DateTime? From,
    DateTime? To) : IRequest<PagedResult<EventLogDto>>;

public class GetEventLogQueryHandler : IRequestHandler<GetEventLogQuery, PagedResult<EventLogDto>>
{
    private readonly IEventLogRepository _eventLogRepository;

    public GetEventLogQueryHandler(IEventLogRepository eventLogRepository)
    {
        _eventLogRepository = eventLogRepository;
    }

    public async Task<PagedResult<EventLogDto>> Handle(GetEventLogQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _eventLogRepository.GetPagedAsync(
            request.Page, request.PageSize, request.DeviceId, request.EventType, request.From, request.To, cancellationToken);

        var dtos = items
            .Select(e => new EventLogDto
            {
                Id = e.Id,
                DeviceId = e.DeviceId,
                DeviceName = e.Device?.Name,
                EventType = e.EventType.ToString(),
                Message = e.Message,
                OccurredAt = e.OccurredAt
            })
            .ToList();

        return PagedResult<EventLogDto>.Create(dtos, request.Page, request.PageSize, totalCount);
    }
}
