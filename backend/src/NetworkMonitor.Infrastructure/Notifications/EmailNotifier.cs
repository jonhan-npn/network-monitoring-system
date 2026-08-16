using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Enums;

namespace NetworkMonitor.Infrastructure.Notifications;

/// <summary>
/// One IAlertDispatcher per channel (see IAlertDispatcher's doc comment) —
/// this one handles AlertChannel.Email via SMTP through MailKit.
/// </summary>
public class EmailNotifier : IAlertDispatcher
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailNotifier> _logger;

    public AlertChannel Channel => AlertChannel.Email;

    public EmailNotifier(IOptions<SmtpSettings> settings, ILogger<EmailNotifier> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(AlertMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            email.To.Add(MailboxAddress.Parse(message.Destination));
            email.Subject = message.Subject;
            email.Body = new TextPart("plain") { Text = message.Body };

            using var client = new MailKit.Net.Smtp.SmtpClient();

            var socketOptions = _settings.UseStartTls
                ? MailKit.Security.SecureSocketOptions.StartTls
                : MailKit.Security.SecureSocketOptions.Auto;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(email, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            // Never let a failed alert send take down the Worker's polling
            // loop — log and report failure so AlertThresholdEngine can
            // decide whether/how to retry, rather than throwing.
            _logger.LogError(ex, "Failed to send email alert for rule {RuleId} to {Destination}", message.RuleId, message.Destination);
            return false;
        }
    }
}
