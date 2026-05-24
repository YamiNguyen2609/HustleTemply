using HustleTemply.Models;

namespace HustleTemply.Services;

public interface ISheetExportService
{
    /// <summary>
    /// Copies the source spreadsheet, shares the copy with the recipient, and returns export data.
    /// </summary>
    /// <param name="request">Sheet ID and recipient email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Copy metadata. <c>EmailSent</c> is true when SMTP notification succeeds.</returns>
    Task<CopyAndEmailResponse> CopyAndEmailAsync(
        CopyAndEmailRequest request,
        CancellationToken cancellationToken = default);
}
