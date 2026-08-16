using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Infrastructure.Authentication;

/// <summary>BCrypt via BCrypt.Net-Next. Work factor 12 is a reasonable
/// default for 2026 hardware — bump it over time as hardware gets faster,
/// this isn't a "set once forever" number.</summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: WorkFactor);

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Malformed/legacy hash in the DB — treat as a failed login
            // rather than throwing 500s at users who happen to hit a bad row.
            return false;
        }
    }
}
