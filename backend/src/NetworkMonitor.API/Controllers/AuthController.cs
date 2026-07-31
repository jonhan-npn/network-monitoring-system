using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.Features.Authentication;

namespace NetworkMonitor.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Exchanges a username/password for an access token + refresh token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await _authenticationService.LoginAsync(request.Username, request.Password, ct);

        if (result is null)
        {
            _logger.LogWarning("Failed login attempt for username {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Registers a new user. In most enterprise deployments this would be
    /// restricted to Admins after the first bootstrap account exists —
    /// gate it behind [Authorize(Policy = "AdminOnly")] once you have one.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await _authenticationService.RegisterAsync(request.Username, request.Email, request.Password, ct);
        return CreatedAtAction(nameof(Login), null, result);
    }

    /// <summary>
    /// Trades a still-valid refresh token for a new access token, so the
    /// Angular client can stay logged in without re-prompting for a password.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Refresh([FromBody] RefreshRequestDto request, CancellationToken ct)
    {
        var result = await _authenticationService.RefreshAsync(request.RefreshToken, ct);

        if (result is null)
        {
            return Unauthorized(new { message = "Refresh token is invalid or expired." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Revokes the caller's current refresh token (server-side logout).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request, CancellationToken ct)
    {
        await _authenticationService.RevokeRefreshTokenAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>Returns the authenticated caller's profile, for the Angular shell to show "logged in as ...".</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public ActionResult<CurrentUserDto> Me()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
        var username = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Ok(new CurrentUserDto(userId ?? string.Empty, username ?? string.Empty, roles));
    }
}

public record LoginRequestDto(string Username, string Password);
public record RegisterRequestDto(string Username, string Email, string Password);
public record RefreshRequestDto(string RefreshToken);
public record CurrentUserDto(string UserId, string Username, string[] Roles);
