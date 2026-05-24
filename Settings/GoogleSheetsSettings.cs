using Microsoft.Extensions.Configuration;

namespace HustleTemply.Settings;

public class GoogleSheetsSettings
{
    public const string SectionName = "GoogleSheets";

    public string SpreadsheetId { get; set; } = string.Empty;

    public string SheetName { get; set; } = "Sheet1";

    public string ServiceAccountKeyPath { get; set; } = string.Empty;

    public ServiceAccountJsonSettings? ServiceAccountJson { get; set; }
}

public class ServiceAccountJsonSettings
{
    [ConfigurationKeyName("type")]
    public string Type { get; set; } = string.Empty;

    [ConfigurationKeyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [ConfigurationKeyName("private_key_id")]
    public string PrivateKeyId { get; set; } = string.Empty;

    [ConfigurationKeyName("private_key")]
    public string PrivateKey { get; set; } = string.Empty;

    [ConfigurationKeyName("client_email")]
    public string ClientEmail { get; set; } = string.Empty;

    [ConfigurationKeyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [ConfigurationKeyName("auth_uri")]
    public string AuthUri { get; set; } = string.Empty;

    [ConfigurationKeyName("token_uri")]
    public string TokenUri { get; set; } = string.Empty;

    [ConfigurationKeyName("auth_provider_x509_cert_url")]
    public string AuthProviderX509CertUrl { get; set; } = string.Empty;

    [ConfigurationKeyName("client_x509_cert_url")]
    public string ClientX509CertUrl { get; set; } = string.Empty;

    [ConfigurationKeyName("universe_domain")]
    public string UniverseDomain { get; set; } = string.Empty;
}
