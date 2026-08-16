namespace NetworkMonitor.Infrastructure.Notifications;

/// <summary>Bound from "Alerts:Smtp" — Alerts__Smtp__Host etc. in docs/deployment.md.</summary>
public class SmtpSettings
{
    public const string SectionName = "Alerts:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "alerts@example.com";
    public string FromName { get; set; } = "Network Monitor";
    public bool UseStartTls { get; set; } = true;
}
