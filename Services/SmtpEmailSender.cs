using System.Net;
using System.Net.Mail;
using HustleTemply.Settings;
using Microsoft.Extensions.Options;

namespace HustleTemply.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpSettings> options,
        ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendCopyNotificationAsync(
        string toEmail,
        string copiedSpreadsheetUrl,
        string sourceSpreadsheetId,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("SMTP sending is disabled (Smtp:Enabled = false)");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Username)
            || string.IsNullOrWhiteSpace(_settings.AppPassword)
            || string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            _logger.LogWarning(
                "SMTP is enabled but Smtp:Username, Smtp:AppPassword, or Smtp:SenderEmail is not configured");
            return false;
        }

        try
        {
            using var message = BuildMessage(toEmail, copiedSpreadsheetUrl, sourceSpreadsheetId);
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.AppPassword)
            };

            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Copy notification email sent to {Email} via SMTP", toEmail);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP failed to send copy notification to {Email}", toEmail);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending copy notification to {Email}", toEmail);
            return false;
        }
    }

    private MailMessage BuildMessage(string toEmail, string copiedSpreadsheetUrl, string sourceSpreadsheetId)
    {
        var from = string.IsNullOrWhiteSpace(_settings.SenderName)
            ? new MailAddress(_settings.SenderEmail)
            : new MailAddress(_settings.SenderEmail, _settings.SenderName);

        var message = new MailMessage
        {
            From = from,
            Subject = _settings.Subject
        };
        message.To.Add(toEmail);

        var plainBody = $"""
            Hello,

            Your spreadsheet copy is ready.

            Open your copy: {copiedSpreadsheetUrl}

            Source spreadsheet ID: {sourceSpreadsheetId}

            — HustleTemply
            """;

        var htmlBody = $"""
            <!DOCTYPE html>
            <html>
            <body>
              <p>Hello,</p>
              <p>Your spreadsheet copy is ready.</p>
              <p><a href="{copiedSpreadsheetUrl}">Open your copy</a></p>
              <p><small>Source spreadsheet ID: {sourceSpreadsheetId}</small></p>
              <p>— HustleTemply</p>
            </body>
            </html>
            """;

        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainBody, null, "text/plain"));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));

        return message;
    }
}
