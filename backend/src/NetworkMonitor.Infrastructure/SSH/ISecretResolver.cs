namespace NetworkMonitor.Infrastructure.SSH;

/// <summary>
/// Resolves a Device.SshCredentialRef pointer (never a raw password — see
/// docs/database.md) into the actual secret at connection time. Kept as its
/// own small interface local to Infrastructure rather than added to
/// Application's ISshService contract, since "how secrets are stored" is
/// purely an infrastructure concern the Application/Worker layers don't
/// need to know about.
/// </summary>
public interface ISecretResolver
{
    /// <param name="credentialRef">
    /// Convention used by this implementation: "env:VAR_NAME" resolves from
    /// an environment variable. Swap this implementation for one backed by
    /// Docker secrets, HashiCorp Vault, or a cloud secret manager without
    /// touching any calling code — that's the point of the interface.
    /// </param>
    string? Resolve(string? credentialRef);
}

public class EnvironmentVariableSecretResolver : ISecretResolver
{
    private const string EnvPrefix = "env:";

    public string? Resolve(string? credentialRef)
    {
        if (string.IsNullOrWhiteSpace(credentialRef))
        {
            return null;
        }

        if (!credentialRef.StartsWith(EnvPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Credential ref '{credentialRef}' doesn't use the 'env:' convention this resolver supports. " +
                "Swap in a different ISecretResolver implementation if you're using Vault/Docker secrets/etc.");
        }

        var varName = credentialRef[EnvPrefix.Length..];
        return Environment.GetEnvironmentVariable(varName);
    }
}
