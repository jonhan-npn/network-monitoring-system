using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Infrastructure.Data.Configurations;

public class ResourceSampleConfiguration : IEntityTypeConfiguration<ResourceSample>
{
    public void Configure(EntityTypeBuilder<ResourceSample> builder)
    {
        builder.ToTable("resource_samples");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(r => r.DeviceId).HasColumnName("device_id");
        builder.Property(r => r.CpuPercent).HasColumnName("cpu_percent");
        builder.Property(r => r.MemoryPercent).HasColumnName("memory_percent");
        builder.Property(r => r.SampledAt).HasColumnName("sampled_at");

        builder.HasIndex(r => new { r.DeviceId, r.SampledAt }).IsDescending(false, true);
    }
}
