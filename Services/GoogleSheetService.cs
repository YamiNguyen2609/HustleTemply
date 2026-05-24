using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using HustleTemply.Models;
using HustleTemply.Parsers;
using HustleTemply.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HustleTemply.Services;

public class GoogleSheetService : IGoogleSheetService
{
    private const string DefaultCacheKey = "sheet-data";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly GoogleSheetsSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleSheetService> _logger;
    private readonly SheetsService _sheetsService;

    public GoogleSheetService(
        IOptions<GoogleSheetsSettings> options,
        IMemoryCache cache,
        ILogger<GoogleSheetService> logger)
    {
        _settings = options.Value;
        _cache = cache;
        _logger = logger;

        var credential = GoogleCredentialFactory.Create(_settings);
        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Hustle-Temply"
        });
    }

    /// <inheritdoc />
    public Task<SheetDataResponse> GetAllDataAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SpreadsheetId))
        {
            throw new InvalidOperationException("GoogleSheets:SpreadsheetId is not configured.");
        }

        return GetAllDataAsync(_settings.SpreadsheetId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SheetDataResponse> GetAllDataAsync(string spreadsheetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new ArgumentException("Spreadsheet ID is required.", nameof(spreadsheetId));
        }

        var cacheKey = GetCacheKey(spreadsheetId);
        if (_cache.TryGetValue(cacheKey, out SheetDataResponse? cached) && cached is not null)
        {
            _logger.LogDebug("Returning sheet data from cache for {SpreadsheetId}", spreadsheetId);
            return cached;
        }

        var sheetName = string.IsNullOrWhiteSpace(_settings.SheetName)
            ? "Sheet1"
            : _settings.SheetName;

        var range = $"{sheetName}!A:Z";
        _logger.LogInformation(
            "Fetching Google Sheet data from spreadsheet {SpreadsheetId}, range {Range}",
            spreadsheetId,
            range);

        var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var rows = response.Values ?? [];
        _logger.LogInformation("Fetched {RowCount} rows from Google Sheet {SpreadsheetId}", rows.Count, spreadsheetId);

        _logger.LogDebug("Parsing sheet rows into structured data");
        var data = GoogleSheetParser.Parse(rows);

        _cache.Set(cacheKey, data, CacheDuration);
        _logger.LogInformation(
            "Sheet data for {SpreadsheetId} parsed and cached for {Minutes} minutes",
            spreadsheetId,
            CacheDuration.TotalMinutes);

        return data;
    }

    private static string GetCacheKey(string spreadsheetId) => $"{DefaultCacheKey}:{spreadsheetId}";
}
