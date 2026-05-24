using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4;
using HustleTemply.Settings;

namespace HustleTemply.Services;

internal static class GoogleCredentialFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Creates a credential scoped for read-only Sheets access.
    /// </summary>
    public static GoogleCredential Create(GoogleSheetsSettings settings)
        => Create(settings, SheetsService.Scope.SpreadsheetsReadonly);

    /// <summary>
    /// Creates a scoped Google credential from file path or embedded service account JSON.
    /// </summary>
    public static GoogleCredential Create(GoogleSheetsSettings settings, params string[] scopes)
    {
        var scopeList = scopes is { Length: > 0 }
            ? scopes
            : [SheetsService.Scope.SpreadsheetsReadonly];

        if (!string.IsNullOrWhiteSpace(settings.ServiceAccountKeyPath)
            && File.Exists(settings.ServiceAccountKeyPath))
        {
            var credential = CredentialFactory.FromFile<ServiceAccountCredential>(settings.ServiceAccountKeyPath);
            return credential.ToGoogleCredential().CreateScoped(scopeList);
        }

        if (settings.ServiceAccountJson is { PrivateKey: not null and not "", ClientEmail: not null and not "" })
        {
            var json = JsonSerializer.Serialize(settings.ServiceAccountJson, JsonOptions);
            var credential = CredentialFactory.FromJson<ServiceAccountCredential>(json);
            return credential.ToGoogleCredential().CreateScoped(scopeList);
        }

        throw new InvalidOperationException(
            "Google Sheets credentials are not configured. Set GoogleSheets:ServiceAccountKeyPath to a valid key file, " +
            "or provide GoogleSheets:ServiceAccountJson in configuration.");
    }

    /// <summary>
    /// Scopes required for Drive copy and share operations.
    /// </summary>
    public static readonly string[] DriveScopes = [DriveService.Scope.Drive];
}
