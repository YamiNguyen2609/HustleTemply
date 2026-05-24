using HustleTemply.Models;

namespace HustleTemply.Services;

public interface IGoogleSheetService
{
    /// <summary>
    /// Fetches and parses sheet data using <c>GoogleSheets:SpreadsheetId</c> from configuration.
    /// </summary>
    Task<SheetDataResponse> GetAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches and parses sheet data for a specific spreadsheet (verify before copy).
    /// </summary>
    /// <param name="spreadsheetId">Google Spreadsheet ID.</param>
    Task<SheetDataResponse> GetAllDataAsync(string spreadsheetId, CancellationToken cancellationToken = default);
}
