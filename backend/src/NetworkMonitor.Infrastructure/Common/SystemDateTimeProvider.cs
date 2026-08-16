using NetworkMonitor.Application.Interfaces;

namespace NetworkMonitor.Infrastructure.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
