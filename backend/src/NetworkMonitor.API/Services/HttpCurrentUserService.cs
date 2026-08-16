using System.Security.Claims;
using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.API.Services;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub"), out var id) ? id : null;
    public string? Username => User?.Identity?.Name ?? User?.FindFirstValue("unique_name");
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
}
