namespace NetworkMonitor.Application.Interfaces;

/// <summary>
/// Thin wrapper over DateTime.UtcNow so handlers and services are
/// unit-testable without depending on the system clock directly.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
