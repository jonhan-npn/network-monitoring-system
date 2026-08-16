namespace NetworkMonitor.Application.Interfaces;

public record SshInventoryResult(
    bool Success,
    string? RawOutput,
    string? Hostname,
    string? FirmwareVersion,
    string? SerialNumber,
    string? ErrorMessage);

public record SshTarget(
    Guid DeviceId,
    string IpAddress,
    int Port,
    string Username,
    string CredentialRef); // resolved to an actual secret by Infrastructure, never held as plaintext here

/// <summary>
/// Abstraction over SSH command execution used to pull inventory info
/// (hostname, firmware/version, serial number) from switches and routers —
/// e.g. running "show version" on Cisco-like devices. Implemented by
/// NetworkMonitor.Infrastructure.SSH.SshInventoryService using SSH.NET.
/// </summary>
public interface ISshService
{
    Task<SshInventoryResult> FetchInventoryAsync(SshTarget target, CancellationToken cancellationToken = default);
}
