using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Infrastructure.Notifications;

/// <summary>
/// AlertChannel.Telegram via a raw call to the Telegram Bot API's
/// sendMessage endpoint (https://core.telegram.org/bots/api#sendmessage).
/// Deliberately not using a Telegram SDK package — the API is one JSON
/// POST, and this project's stated learning goals include "REST APIs," so
/// hand-rolling this one call is more instructive than pulling in a library
/// for it. AlertMessage.Destination is expected to be the target chat id
/// (a group chat id looks like "-1001234567890"; a DM to the bot looks
/// like a positive user id — see docs/api.md's alert rule example).
/// </summary>
public class TelegramNotifier : IAlertDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramNotifier> _logger;

    public AlertChannel Channel => AlertChannel.Telegram;

    public TelegramNotifier(HttpClient httpClient, IOptions<TelegramSettings> settings, ILogger<TelegramNotifier> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(AlertMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BotToken))
        {
            _logger.LogWarning("Telegram alert for rule {RuleId} skipped — Alerts:Telegram:BotToken is not configured.", message.RuleId);
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage";

            var payload = new
            {
                chat_id = message.Destination,
                text = $"{message.Subject}\n\n{message.Body}",
                parse_mode = "HTML"
            };

            using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Telegram alert for rule {RuleId} failed: {StatusCode} {Body}",
                    message.RuleId, (int)response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram alert for rule {RuleId} to chat {Destination}", message.RuleId, message.Destination);
            return false;
        }
    }
}
