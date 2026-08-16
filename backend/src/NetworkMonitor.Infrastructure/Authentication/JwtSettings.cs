namespace NetworkMonitor.Infrastructure.Authentication;

/// <summary>
/// Bound from the "Jwt" configuration section (Jwt__Issuer, Jwt__Audience,
/// Jwt__SigningKey, Jwt__ExpiryMinutes — see docs/deployment.md's
/// environment variables list). Registered via
/// services.Configure&lt;JwtSettings&gt;(configuration.GetSection("Jwt"))
/// in DependencyInjection.AddInfrastructure.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Must be at least 32 bytes/256 bits for HMAC-SHA256 — see the guard in JwtTokenGenerator.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 30;
}
