using HustleTemply.Models;
using HustleTemply.Settings;
using Microsoft.Extensions.Options;

namespace HustleTemply.Services;

public class SheetExportService : ISheetExportService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SheetExportService> _logger;

    public SheetExportService(
        IEmailSender emailSender,
        ILogger<SheetExportService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CopyAndEmailResponse> CopyAndEmailAsync(
        CopyAndEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var sheetId = request.SheetId.Trim();
        var email = request.Email.Trim();
        var url = $"https://docs.google.com/spreadsheets/d/{sheetId}/copy";

        var emailSent = await _emailSender.SendCopyNotificationAsync(
            email,
            url,
            sheetId,
            cancellationToken).ConfigureAwait(false);

        if (!emailSent)
        {
            _logger.LogWarning(
                "Notification email was not sent to {Email}",
                email);
        }

        return new CopyAndEmailResponse(
            sheetId,
            url,
            email,
            emailSent);
    }
}
