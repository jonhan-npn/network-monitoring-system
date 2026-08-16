using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Interfaces;

public record GeneratedToken(string AccessToken, DateTime ExpiresAt);

/// <summary>Implemented by Infrastructure.Authentication.JwtTokenGenerator.</summary>
public interface IJwtTokenGenerator
{
    GeneratedToken GenerateToken(User user);
}

/// <summary>Implemented by Infrastructure.Authentication.PasswordHasher (BCrypt).</summary>
public interface IPasswordHasher
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string passwordHash);
}

/// <summary>
/// Reads the authenticated user's identity out of the current HTTP context.
/// Implemented in the API project (it wraps IHttpContextAccessor), which is
/// why this interface lives in Application rather than Infrastructure —
/// Infrastructure has no concept of "current request."
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
