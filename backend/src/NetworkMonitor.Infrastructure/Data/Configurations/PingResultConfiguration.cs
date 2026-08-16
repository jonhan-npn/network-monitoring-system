using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Infrastructure.Data.Configurations;

public class PingResultConfiguration : IEntityTypeConfiguration<PingResult>
{
    public void Configure(EntityTypeBuilder<PingResult> builder)
    {
        builder.ToTable("ping_results");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn(); // bigserial

        builder.Property(p => p.DeviceId).HasColumnName("device_id");
        builder.Property(p => p.IsOnline).HasColumnName("is_online");
        builder.Property(p => p.LatencyMs).HasColumnName("latency_ms");
        builder.Property(p => p.CheckedAt).HasColumnName("checked_at");

        // The one index that matters most for dashboard responsiveness —
        // see docs/database.md "Indexing rationale".
        builder.HasIndex(p => new { p.DeviceId, p.CheckedAt }).IsDescending(false, true);
    }
}
