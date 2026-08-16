using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Infrastructure.Data.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DeviceId).HasColumnName("device_id");
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(128).IsRequired();

        // Plain string, not an enum — see docs/database.md and
        // Domain.Entities.AlertRule's doc comment for the rationale.
        builder.Property(a => a.Metric).HasColumnName("metric").HasMaxLength(32).IsRequired();

        builder.Property(a => a.ThresholdPercent).HasColumnName("threshold_percent");
        builder.Property(a => a.ConsecutiveBreaches).HasColumnName("consecutive_breaches");

        builder.Property(a => a.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(a => a.Destination).HasColumnName("destination").HasMaxLength(256).IsRequired();
        builder.Property(a => a.IsEnabled).HasColumnName("is_enabled");

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.LastTriggeredAt).HasColumnName("last_triggered_at");

        builder.HasIndex(a => a.DeviceId);
    }
}
