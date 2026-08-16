using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Worker;

/// <summary>
/// The Worker runs as a trusted background system, not on behalf of any
/// logged-in user — there's no HTTP request/JWT to read identity from
/// (contrast with the API's ICurrentUserService, which wraps
/// IHttpContextAccessor). This stub satisfies the interface for any
/// Application-layer code that resolves ICurrentUserService, always
/// reporting "not authenticated." Nothing the Worker actually calls today
/// (RecordPingResultCommand, etc.) uses ICurrentUserService — this exists
/// purely so the DI container has something to hand out if that ever changes.
/// </summary>
public class SystemCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public string? Username => "system-worker";
    public string? Role => null;
    public bool IsAuthenticated => false;
}
