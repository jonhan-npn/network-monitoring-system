using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Features.Authentication;
using NetworkMonitor.Application.Features.Users;

namespace NetworkMonitor.API.Controllers;

/// <summary>
/// Authentication endpoints. Login/register/me go through the same
/// MediatR pipeline as every other controller; the access token itself is
/// issued by IJwtTokenGenerator in Infrastructure.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Exchanges a username/password for an access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct) =>
        Ok(await sender.Send(new LoginCommand(request.Username, request.Password), ct));

    /// <summary>
    /// Registers a new user and logs them straight in. Self-registration
    /// always gets the least-privileged role (Viewer); promoting a user is
    /// an Admin-only action through the user-management endpoints.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        await sender.Send(new CreateUserCommand(request.Username, request.Email, request.Password, Role: "Viewer"), ct);
        var result = await sender.Send(new LoginCommand(request.Username, request.Password), ct);
        return CreatedAtAction(nameof(Login), null, result);
    }

    /// <summary>
    /// Trades a refresh token for a new access token. Not implemented in v1:
    /// refresh tokens need a server-side store (and a schema change), which
    /// docs/api.md deliberately defers. Clients should re-login when the
    /// access token expires.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult Refresh(CancellationToken ct) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "Refresh tokens are not implemented yet. Please log in again." });

    /// <summary>
    /// Client-side logout. JWTs are stateless, so there is nothing to revoke
    /// server-side until refresh tokens exist — the Angular app drops its
    /// stored token. Kept as an endpoint so the client contract is stable.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout() => NoContent();

    /// <summary>Returns the authenticated caller's profile, for the Angular shell to show "logged in as ...".</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        Ok(await sender.Send(new GetCurrentUserQuery(), ct));
}

public record LoginRequestDto(string Username, string Password);
public record RegisterRequestDto(string Username, string Email, string Password);
