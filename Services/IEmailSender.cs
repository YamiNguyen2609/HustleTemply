namespace HustleTemply.Services;

public interface IEmailSender
{
    /// <summary>
    /// Sends a copy notification email with spreadsheet link.
    /// </summary>
    /// <returns><c>true</c> if sent successfully; <c>false</c> if disabled or send failed.</returns>
    Task<bool> SendCopyNotificationAsync(
        string toEmail,
        string copiedSpreadsheetUrl,
        string sourceSpreadsheetId,
        CancellationToken cancellationToken = default);
}
