using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default);

    /// <summary>All accounts, ordered by username. Not paginated — user
    /// management tables stay small (tens, not thousands, of accounts) for
    /// this kind of internal tool, so pagination would be premature here.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
