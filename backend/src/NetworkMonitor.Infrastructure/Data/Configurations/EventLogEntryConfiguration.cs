using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Infrastructure.Data.Configurations;

public class EventLogEntryConfiguration : IEntityTypeConfiguration<EventLogEntry>
{
    public void Configure(EntityTypeBuilder<EventLogEntry> builder)
    {
        builder.ToTable("event_log_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(e => e.DeviceId).HasColumnName("device_id");

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(1024).IsRequired();

        // Maps to the `jsonb` column. Npgsql's provider recognizes `jsonb`
        // via this column type annotation on a plain string property — no
        // separate value converter needed for the "raw JSON string" shape
        // this project uses (see EventLogEntry.Metadata's doc comment).
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(e => new { e.DeviceId, e.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(e => e.EventType);
    }
}
