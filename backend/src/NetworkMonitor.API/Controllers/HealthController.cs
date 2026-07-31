using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Infrastructure.Data;

namespace NetworkMonitor.API.Controllers;

/// <summary>
/// Simple, unauthenticated health endpoints for Docker healthchecks / Nginx
/// upstream checks. The richer /health endpoint (with DB check) is also
/// mapped via MapHealthChecks in Program.cs — this controller exists for a
/// human-readable version and a fast liveness ping.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Liveness: is the process up at all? Used for Docker HEALTHCHECK.</summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live() => Ok(new { status = "alive", timestampUtc = DateTimeOffset.UtcNow });

    /// <summary>Readiness: can the API actually reach Postgres? Used before Nginx routes traffic to it.</summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(ct);
            if (!canConnect)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "database unreachable" });
            }

            return Ok(new { status = "ready", timestampUtc = DateTimeOffset.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "error", detail = ex.Message });
        }
    }
}
