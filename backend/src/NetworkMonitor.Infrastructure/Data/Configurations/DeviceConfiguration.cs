using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Infrastructure.Data.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(128).IsRequired();

        // Stored as text; Postgres's native `inet` type isn't something EF
        // Core maps to out of the box without a value converter + a custom
        // Npgsql type mapping. Keeping it a validated string here (validation
        // lives in CreateDeviceCommandValidator) is simpler and matches what
        // most EF/Postgres projects actually do in practice.
        builder.Property(d => d.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsRequired();

        builder.Property(d => d.DeviceType)
            .HasColumnName("device_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(d => d.Location).HasColumnName("location").HasMaxLength(256);
        builder.Property(d => d.Description).HasColumnName("description").HasMaxLength(512);

        builder.Property(d => d.SnmpCommunity).HasColumnName("snmp_community").HasMaxLength(128);
        builder.Property(d => d.SnmpPort).HasColumnName("snmp_port");
        builder.Property(d => d.SnmpVersion).HasColumnName("snmp_version").HasMaxLength(8);

        builder.Property(d => d.SshPort).HasColumnName("ssh_port");
        builder.Property(d => d.SshUsername).HasColumnName("ssh_username").HasMaxLength(128);
        builder.Property(d => d.SshCredentialRef).HasColumnName("ssh_credential_ref").HasMaxLength(256);

        builder.Property(d => d.IsMonitored).HasColumnName("is_monitored");

        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(d => d.IpAddress).IsUnique();
        builder.HasIndex(d => d.DeviceType);
        builder.HasIndex(d => d.IsMonitored);

        builder.HasMany(d => d.PingResults)
            .WithOne(p => p.Device)
            .HasForeignKey(p => p.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.ResourceSamples)
            .WithOne(r => r.Device)
            .HasForeignKey(r => r.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.EventLogEntries)
            .WithOne(e => e.Device)
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(d => d.AlertRules)
            .WithOne(a => a.Device)
            .HasForeignKey(a => a.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
