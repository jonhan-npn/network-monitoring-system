using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Infrastructure.Data;

/// <summary>
/// Shared by both the API and the Worker — see docs/architecture.md for why
/// two processes can safely share one DbContext type (they never talk to
/// each other directly, only through Postgres).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PingResult> PingResults => Set<PingResult>();
    public DbSet<ResourceSample> ResourceSamples => Set<ResourceSample>();
    public DbSet<EventLogEntry> EventLogEntries => Set<EventLogEntry>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in Data/Configurations/
        // instead of configuring entities inline here, so this file doesn't
        // grow without bound as more entities are added.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
