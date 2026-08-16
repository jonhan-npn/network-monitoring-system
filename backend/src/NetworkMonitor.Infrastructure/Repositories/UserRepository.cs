using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;
using NetworkMonitor.Infrastructure.Data;

namespace NetworkMonitor.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<bool> ExistsWithUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default) =>
        _db.Users.AnyAsync(u => u.Username == username || u.Email == email, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _db.Users.AddAsync(user, cancellationToken);

    public void Update(User user) => _db.Users.Update(user);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
