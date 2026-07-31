using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.Features.Devices.Commands;
using NetworkMonitor.Application.Features.Devices.Queries;
using NetworkMonitor.Application.DTOs;

namespace NetworkMonitor.API.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceQueryService _queryService;
    private readonly IDeviceCommandService _commandService;

    public DevicesController(IDeviceQueryService queryService, IDeviceCommandService commandService)
    {
        _queryService = queryService;
        _commandService = commandService;
    }

    /// <summary>Lists devices, optionally filtered by type or free-text search.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeviceDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? type,
        CancellationToken ct)
    {
        var devices = await _queryService.GetDevicesAsync(search, type, ct);
        return Ok(devices);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeviceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var device = await _queryService.GetDeviceByIdAsync(id, ct);
        return device is null ? NotFound() : Ok(device);
    }

    /// <summary>Adds a new switch/router/host to the inventory. Ping monitoring for it starts on the next worker cycle.</summary>
    [HttpPost]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceDto>> Create([FromBody] CreateDeviceRequest request, CancellationToken ct)
    {
        var created = await _commandService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> Update(Guid id, [FromBody] UpdateDeviceRequest request, CancellationToken ct)
    {
        var updated = await _commandService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _commandService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Triggers an immediate out-of-band ping, useful for "test connection" buttons in the Angular device form.</summary>
    [HttpPost("{id:guid}/ping-now")]
    [ProducesResponseType(typeof(PingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PingResultDto>> PingNow(Guid id, CancellationToken ct)
    {
        var result = await _commandService.PingNowAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public record CreateDeviceRequest(string Name, string IpAddress, string Type, string? SnmpCommunity, string? SshUsername, string? SshPassword, int? SshPort);
public record UpdateDeviceRequest(string Name, string IpAddress, string Type, string? SnmpCommunity, string? SshUsername, string? SshPassword, int? SshPort, bool IsMonitoringEnabled);
