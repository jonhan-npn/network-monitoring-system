using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.Common.Models;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Devices.Commands;
using NetworkMonitor.Application.Features.Devices.Queries;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.API.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<DeviceListItemDto>>> GetAll([FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] string? type = null,
        [FromQuery] bool? isMonitored = null, CancellationToken ct = default)
    {
        // `type` is an optional filter: a missing value means "all types",
        // so only validate it when the caller actually supplied one.
        DeviceType? deviceType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!TryParseDeviceType(type, out var parsed)) return ValidationProblem("Type is not a valid device type.");
            deviceType = parsed;
        }

        return Ok(await sender.Send(new GetDevicesQuery(Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), search, deviceType, isMonitored), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeviceDto>> GetById(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetDeviceByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<DeviceDto>> Create(DeviceRequest request, CancellationToken ct)
    {
        if (!TryParseDeviceType(request.DeviceType, out var type)) return ValidationProblem("DeviceType is required and must be valid.");
        var created = await sender.Send(ToCreateCommand(request, type), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<DeviceDto>> Update(Guid id, DeviceRequest request, CancellationToken ct)
    {
        if (!TryParseDeviceType(request.DeviceType, out var type)) return ValidationProblem("DeviceType is required and must be valid.");
        return Ok(await sender.Send(new UpdateDeviceCommand(id, request.Name, request.IpAddress, type, request.Location,
            request.Description, request.SnmpCommunity, request.SnmpPort, request.SnmpVersion, request.SshPort,
            request.SshUsername, request.SshCredentialRef), ct));
    }

    [HttpPatch("{id:guid}/monitoring")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<IActionResult> SetMonitoring(Guid id, SetMonitoringRequest request, CancellationToken ct)
    {
        await sender.Send(new SetDeviceMonitoringCommand(id, request.IsMonitored), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteDeviceCommand(id), ct);
        return NoContent();
    }

    private static CreateDeviceCommand ToCreateCommand(DeviceRequest r, DeviceType type) => new(r.Name, r.IpAddress, type,
        r.Location, r.Description, r.SnmpCommunity, r.SnmpPort, r.SnmpVersion, r.SshPort, r.SshUsername, r.SshCredentialRef);
    private static bool TryParseDeviceType(string? type, out DeviceType value)
    {
        value = default;
        return !string.IsNullOrWhiteSpace(type)
            && Enum.TryParse(type, true, out value)
            && Enum.IsDefined(value);
    }
}

public record DeviceRequest(string Name, string IpAddress, string DeviceType, string? Location, string? Description,
    string SnmpCommunity = "public", int SnmpPort = 161, string SnmpVersion = "2c", int SshPort = 22,
    string? SshUsername = null, string? SshCredentialRef = null);
public record SetMonitoringRequest(bool IsMonitored);
