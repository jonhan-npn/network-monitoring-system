using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Application.Interfaces;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace NetworkMonitor.Infrastructure.SSH;

/// <summary>
/// ISshService via SSH.NET. Runs a short list of common inventory commands
/// and keeps whichever one succeeds — device CLIs vary too much (Cisco IOS
/// vs NX-OS vs Juniper Junos vs a plain Linux box) to assume one command
/// works everywhere. Parsing of hostname/firmware/serial is intentionally
/// best-effort regex over common formats; RawOutput always carries the
/// full response so callers/logs aren't dependent on the parser guessing
/// right for gear this hasn't been tested against yet.
/// </summary>
public class SshInventoryService : ISshService
{
    // Tried in order; first one that doesn't error is used. "show version"
    // covers most Cisco-like network gear; "uname -a" covers Linux servers.
    private static readonly string[] CandidateCommands = ["show version", "uname -a"];

    private const int ConnectTimeoutSeconds = 10;
    private const int CommandTimeoutSeconds = 15;

    private readonly ISecretResolver _secretResolver;
    private readonly ILogger<SshInventoryService> _logger;

    public SshInventoryService(ISecretResolver secretResolver, ILogger<SshInventoryService> logger)
    {
        _secretResolver = secretResolver;
        _logger = logger;
    }

    public async Task<SshInventoryResult> FetchInventoryAsync(SshTarget target, CancellationToken cancellationToken = default)
    {
        var password = _secretResolver.Resolve(target.CredentialRef);

        if (string.IsNullOrEmpty(password))
        {
            return new SshInventoryResult(false, null, null, null, null,
                $"Could not resolve a credential for ref '{target.CredentialRef}'.");
        }

        using var client = new SshClient(target.IpAddress, target.Port, target.Username, password);
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(ConnectTimeoutSeconds);

        try
        {
            await Task.Run(client.Connect, cancellationToken);

            foreach (var command in CandidateCommands)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var cmd = client.CreateCommand(command);
                cmd.CommandTimeout = TimeSpan.FromSeconds(CommandTimeoutSeconds);

                var output = await Task.Run(() => cmd.Execute(), cancellationToken);

                if (cmd.ExitStatus == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var (hostname, firmware, serial) = ParseInventory(output);
                    return new SshInventoryResult(true, output, hostname, firmware, serial, null);
                }
            }

            return new SshInventoryResult(false, null, null, null, null,
                $"None of the candidate commands ({string.Join(", ", CandidateCommands)}) succeeded on this device.");
        }
        catch (SshAuthenticationException ex)
        {
            _logger.LogWarning(ex, "SSH authentication failed for device {DeviceId} ({IpAddress})", target.DeviceId, target.IpAddress);
            return new SshInventoryResult(false, null, null, null, null, "Authentication failed.");
        }
        catch (Exception ex) when (ex is SshConnectionException or SshOperationTimeoutException or System.Net.Sockets.SocketException)
        {
            _logger.LogDebug(ex, "SSH connection failed for device {DeviceId} ({IpAddress})", target.DeviceId, target.IpAddress);
            return new SshInventoryResult(false, null, null, null, null, ex.Message);
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    /// <summary>
    /// Best-effort extraction from common CLI output shapes. None of these
    /// patterns are guaranteed to match every vendor — extend as you
    /// actually test against real gear rather than trying to anticipate
    /// every format up front.
    /// </summary>
    private static (string? Hostname, string? Firmware, string? Serial) ParseInventory(string output)
    {
        // Cisco IOS-style: "Software (...), Version 15.2(4)E7, ..."
        var firmwareMatch = Regex.Match(output, @"Version\s+([A-Za-z0-9.\(\)]+)", RegexOptions.IgnoreCase);
        var firmware = firmwareMatch.Success ? firmwareMatch.Groups[1].Value : null;

        // Cisco IOS-style: "System Serial Number : ABC1234D5EF"
        var serialMatch = Regex.Match(output, @"Serial\s*Number\s*[:\-]?\s*([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        var serial = serialMatch.Success ? serialMatch.Groups[1].Value : null;

        // Cisco IOS prompt-style hostname often isn't in "show version" output
        // directly; "uname -a" gives it as the second whitespace-separated
        // token (e.g. "Linux core-switch-01 5.15.0 ...").
        var unameMatch = Regex.Match(output, @"^Linux\s+(\S+)", RegexOptions.IgnoreCase);
        var hostname = unameMatch.Success ? unameMatch.Groups[1].Value : null;

        if (unameMatch.Success)
        {
            firmware ??= Regex.Match(output, @"^Linux\s+\S+\s+(\S+)").Groups[1].Value is { Length: > 0 } kernelVersion
                ? kernelVersion
                : null;
        }

        return (hostname, firmware, serial);
    }
}
