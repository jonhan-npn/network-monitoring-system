using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Domain.Entities;

/// <summary>
/// A monitored network device (switch, router, server, AP, firewall).
/// Maps to the `devices` table (infrastructure/postgres/init.sql).
/// Plain settable properties, not a rich domain model with private setters —
/// this project favors simple, EF-friendly entities over strict encapsulation,
/// since validation lives in the Application layer's FluentValidation
/// validators rather than in entity constructors/methods.
/// </summary>
public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; } = DeviceType.Other;
    public string? Location { get; set; }
    public string? Description { get; set; }

    // SNMP polling config
    public string SnmpCommunity { get; set; } = "public";
    public int SnmpPort { get; set; } = 161;
    public string SnmpVersion { get; set; } = "2c";

    // SSH inventory config — SshCredentialRef points at a secret store
    // entry, never a raw password (see docs/api.md and docs/database.md).
    public int SshPort { get; set; } = 22;
    public string? SshUsername { get; set; }
    public string? SshCredentialRef { get; set; }

    public bool IsMonitored { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (EF Core) — not always loaded; callers that
    // only need the id/name should Include() explicitly rather than relying
    // on lazy loading, which this project does not enable.
    public ICollection<PingResult> PingResults { get; set; } = new List<PingResult>();
    public ICollection<ResourceSample> ResourceSamples { get; set; } = new List<ResourceSample>();
    public ICollection<EventLogEntry> EventLogEntries { get; set; } = new List<EventLogEntry>();
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
}
