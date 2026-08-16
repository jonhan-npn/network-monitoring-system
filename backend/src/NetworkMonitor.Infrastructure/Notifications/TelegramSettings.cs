namespace NetworkMonitor.Infrastructure.Notifications;

/// <summary>Bound from "Alerts:Telegram" — Alerts__Telegram__BotToken in docs/deployment.md.</summary>
public class TelegramSettings
{
    public const string SectionName = "Alerts:Telegram";

    public string BotToken { get; set; } = string.Empty;
}
