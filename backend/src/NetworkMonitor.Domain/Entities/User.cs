namespace NetworkMonitor.Domain.Entities;

/// <summary>
/// A login account. Maps to the `users` table. `Role` is a plain string
/// ("Admin" | "Operator" | "Viewer") rather than an enum so new roles don't
/// require a schema migration — see docs/database.md for the rationale.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer";
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
