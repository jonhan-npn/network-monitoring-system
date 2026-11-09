using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Features.Devices;

public static class DeviceMappingExtensions
{
    public static DeviceDto ToDto(this Device device, bool? isOnline = null, DateTime? lastCheckedAt = null,
        double? lastCpuPercent = null, double? lastMemoryPercent = null) => new()
    {
        Id = device.Id,
        Name = device.Name,
        IpAddress = device.IpAddress,
        DeviceType = device.DeviceType.ToString(),
        Location = device.Location,
        Description = device.Description,
        SnmpCommunity = device.SnmpCommunity,
        SnmpPort = device.SnmpPort,
        SnmpVersion = device.SnmpVersion,
        SshPort = device.SshPort,
        SshUsername = device.SshUsername,
        SshCredentialRef = device.SshCredentialRef,
        IsMonitored = device.IsMonitored,
        CreatedAt = device.CreatedAt,
        UpdatedAt = device.UpdatedAt,
        IsOnline = isOnline,
        LastCheckedAt = lastCheckedAt,
        LastCpuPercent = lastCpuPercent,
        LastMemoryPercent = lastMemoryPercent
    };

    public static DeviceListItemDto ToListItemDto(this Device device, bool? isOnline = null, DateTime? lastCheckedAt = null) => new()
    {
        Id = device.Id,
        Name = device.Name,
        IpAddress = device.IpAddress,
        DeviceType = device.DeviceType.ToString(),
        Location = device.Location,
        Description = device.Description,
        IsMonitored = device.IsMonitored,
        IsOnline = isOnline,
        LastCheckedAt = lastCheckedAt
    };
}
