namespace NetworkMonitor.Application.DTOs;

public class DeviceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Description { get; init; }
    public string SnmpCommunity { get; init; } = "public";
    public int SnmpPort { get; init; } = 161;
    public string SnmpVersion { get; init; } = "2c";
    public int SshPort { get; init; } = 22;
    public string? SshUsername { get; init; }
    public string? SshCredentialRef { get; init; }
    public bool IsMonitored { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Convenience fields for list views — populated from the latest
    // ping_results / resource_samples row rather than stored on Device itself.
    public bool? IsOnline { get; init; }
    public DateTime? LastCheckedAt { get; init; }
    public double? LastCpuPercent { get; init; }
    public double? LastMemoryPercent { get; init; }
}

/// <summary>Shape used by DeviceListDto for the paginated inventory view.</summary>
public class DeviceListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Description { get; init; }
    public bool IsMonitored { get; init; }
    public bool? IsOnline { get; init; }
    public DateTime? LastCheckedAt { get; init; }
}
