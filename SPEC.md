# HustleTemply — Google Sheets REST API Specification

**Status:** Implemented  
**Stack:** C# / .NET 10 (ASP.NET Core Web API)  
**Data source:** Google Sheets API v4  
**Auth:** Google Service Account (JSON key file and/or embedded config)

---

## Project overview

REST API that reads structured data from a Google Sheet and returns mapped JSON (camelCase). Configuration lives in `appsettings.json` under `GoogleSheets`. Sheet rows are parsed by section markers into strongly typed models.

---

## Google Sheet structure

One worksheet (`Sheet1` by default) with sections separated by marker rows in column A.

| Pattern | Meaning |
|---------|---------|
| `{SectionName} Start` | Section begins; **next row** = column headers |
| `{SectionName} End` | Section ends |
| Rows between headers and End | Data rows |

Empty rows (all cells blank) are skipped. Unknown section names are skipped silently.

### Sections and columns

| Section | Columns |
|---------|---------|
| **Configuration** | Id, Name, Value |
| **Payment** | Id, Account Name, Bank Name, Bank Account, Bank QR |
| **Complexity** | Id, Name, Display Name |
| **Category** | Id, Name, Display Name |
| **Social** | Id, Name, Display Name, Url, Active Bio, Active Temply |
| **Benefit** | Id, Name, Description, Icon |
| **Mission** | Id, Name, Description, Icon |
| **Product** | Id, Name, Description, Url, Old Pricing, New Pricing, Thumb, Best Seller, Level, Category |
| **Product Image** | Id, Product Id, Url |

**Type rules**

- Nullable strings: empty → `null`
- Boolean (`Active Bio`, `Active Temply`, `Best Seller`): `"True"` / `"False"` (case-insensitive); missing → `false`
- Decimal (`Old Pricing`, `New Pricing`): empty → `null`
- **Product images:** After parsing, `Product Image` rows are joined to `Product` by `Product Id` → `ProductItem.Images` (`List<string>`)

---

## Configuration (`appsettings.json`)

```json
{
  "GoogleSheets": {
    "SpreadsheetId": "YOUR_SPREADSHEET_ID",
    "SheetName": "Sheet1",
    "ServiceAccountKeyPath": "credentials/service-account.json",
    "ServiceAccountJson": { }
  }
}
```

| Key | Purpose |
|-----|---------|
| `SpreadsheetId` | Google Spreadsheet ID |
| `SheetName` | Worksheet tab name (default `Sheet1`) |
| `ServiceAccountKeyPath` | Path to downloaded service account JSON (preferred for production) |
| `ServiceAccountJson` | Optional embedded credentials (dev fallback) |

### Credential resolution

1. If `ServiceAccountKeyPath` is set and the file exists → load from file, scope `SpreadsheetsReadonly`
2. Else if `ServiceAccountJson` has `private_key` and `client_email` → load from config, same scope
3. Else → `InvalidOperationException` with a clear message

Implementation: `Services/GoogleCredentialFactory.cs` (uses `CredentialFactory` from Google.Apis.Auth).

### `ServiceAccountJson` (not sheet data)

Google IAM service account key — used only for API authentication. **Never** returned by REST endpoints.

```json
"ServiceAccountJson": {
  "type": "service_account",
  "project_id": "your-gcp-project-id",
  "private_key_id": "...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "your-sa@your-project.iam.gserviceaccount.com",
  "client_id": "...",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/...",
  "universe_domain": "googleapis.com"
}
```

Share the spreadsheet with `client_email` (Viewer is enough for read-only).

**Security:** `credentials/*.json` is gitignored. Prefer key file in production; avoid committing `private_key` in `appsettings.json`.

C# binding: `Settings/ServiceAccountJsonSettings.cs` with `[ConfigurationKeyName]` for snake_case JSON keys.

---

## Models (`Models/`)

```csharp
public record ConfigurationItem(string Id, string Name, string? Value);

public record PaymentItem(string Id, string AccountName, string BankName, string BankAccount, string? BankQr);

public record ComplexityItem(string Id, string Name, string DisplayName);

public record CategoryItem(string Id, string Name, string DisplayName);

public record SocialItem(string Id, string Name, string DisplayName, string Url, bool ActiveBio, bool ActiveTemply);

public record BenefitItem(string Id, string Name, string Description, string Icon);

public record MissionItem(string Id, string Name, string Description, string Icon);

public record ProductItem(
    string Id, string Name, string Description, string Url,
    decimal? OldPricing, decimal? NewPricing, string? Thumb,
    bool BestSeller, string Level, string Category,
    List<string> Images);

public record SheetDataResponse(
    List<ConfigurationItem> Configurations,
    List<PaymentItem> Payments,
    List<ComplexityItem> Complexities,
    List<CategoryItem> Categories,
    List<SocialItem> Socials,
    List<BenefitItem> Benefits,
    List<MissionItem> Missions,
    List<ProductItem> Products);
```

---

## Parser (`Parsers/GoogleSheetParser.cs`)

1. Input: all rows from range `{SheetName}!A:J`
2. Iterate top to bottom
3. Column A == `{Section} Start` → enter section; next row = headers (case-insensitive, trimmed)
4. Column A == `{Section} End` → exit section
5. Skip fully empty rows
6. Map data rows to models by header names
7. Join **Product Image** → **Product** on `Product Id`

Known sections: `Configuration`, `Payment`, `Complexity`, `Category`, `Social`, `Benefit`, `Mission`, `Product`, `Product Image`.

---

## Service layer (`Services/`)

```csharp
public interface IGoogleSheetService
{
    Task<SheetDataResponse> GetAllDataAsync(CancellationToken cancellationToken = default);
    Task<SheetDataResponse> GetAllDataAsync(string spreadsheetId, CancellationToken cancellationToken = default);
}
```

- **Implementation:** `GoogleSheetService` (singleton)
- **Export:** `SheetExportService` — Drive copy + share (`Google.Apis.Drive.v3`)
- **Package:** `Google.Apis.Sheets.v4`, `Google.Apis.Drive.v3`
- **Fetch:** `Spreadsheets.Values.Get(spreadsheetId, $"{sheetName}!A:J")`
- **Cache:** `IMemoryCache`, keys `sheet-data:{spreadsheetId}`, duration **5 minutes**
- **Logging:** `ILogger<T>` on fetch, parse, copy, and share

---

## API endpoints (`Controllers/SheetDataController.cs`)

| Method | Route | Response |
|--------|-------|----------|
| GET | `/api/sheet-data` | `SheetDataResponse` (configured `SpreadsheetId`) |
| GET | `/api/sheet-data/{sheetId}` | `SheetDataResponse` (verify before copy) |
| POST | `/api/sheet-data/copy-and-email` | `CopyAndEmailResponse` |
| GET | `/api/sheet-data/configurations` | `List<ConfigurationItem>` |
| GET | `/api/sheet-data/products` | `List<ProductItem>` |
| GET | `/api/sheet-data/categories` | `List<CategoryItem>` |
| GET | `/api/sheet-data/socials` | `List<SocialItem>` |

### Verify and copy (Phase 1)

**Verify:** `GET /api/sheet-data/{sheetId}` — parse and return data for the given spreadsheet. Uses `GoogleSheets:SheetName` for the tab.

**Copy and share:** `POST /api/sheet-data/copy-and-email`

```json
{
  "sheetId": "your-spreadsheet-id",
  "email": "user@example.com"
}
```

Response includes `sourceSpreadsheetId`, `copiedSpreadsheetId` (copy link), `recipientEmail`, and `emailSent` (`true` when SMTP notification succeeds).

If `Smtp:Enabled` is true, a notification email is sent via **Gmail SMTP** (App Password).

### SMTP configuration (Gmail App Password)

```json
"Smtp": {
  "Enabled": true,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your@gmail.com",
  "AppPassword": "your-16-char-app-password",
  "SenderEmail": "your@gmail.com",
  "SenderName": "HustleTemply",
  "Subject": "Your HustleTemply sheet copy"
}
```

| Key | Purpose |
|-----|---------|
| `Enabled` | `false` skips email; `emailSent` is `false` |
| `Username` | Gmail address for SMTP auth |
| `AppPassword` | [Gmail App Password](https://myaccount.google.com/apppasswords) (not your account password) |
| `SenderEmail` | From address (usually same as `Username`) |
| `Host` / `Port` / `EnableSsl` | Default Gmail: `smtp.gmail.com`, `587`, `true` |

Store `AppPassword` in user secrets or environment variables for production — do not commit it.

If SMTP send fails, API still returns `200` with `emailSent: false`.

### Sheet copy configuration

```json
"SheetCopy": {
  "NamePrefix": "Copy - ",
  "ShareRole": "writer"
}
```

**GCP:** Enable **Google Drive API**. Service account needs access to source spreadsheet (Editor recommended for copy).

JSON serialization: **camelCase**. Public controller and service methods include XML doc comments.

Slice endpoints call `GetAllDataAsync` and project a list (cached aggregate).

---

## Project structure

```
/Controllers
    SheetDataController.cs
/Models
    ConfigurationItem.cs
    PaymentItem.cs
    ComplexityItem.cs
    CategoryItem.cs
    SocialItem.cs
    BenefitItem.cs
    MissionItem.cs
    ProductItem.cs
    SheetDataResponse.cs
    CopyAndEmailRequest.cs
    CopyAndEmailResponse.cs
/Services
    IGoogleSheetService.cs
    GoogleSheetService.cs
    ISheetExportService.cs
    SheetExportService.cs
    IEmailSender.cs
    SmtpEmailSender.cs
    GoogleCredentialFactory.cs
/Parsers
    GoogleSheetParser.cs
/Settings
    GoogleSheetsSettings.cs
    SheetCopySettings.cs
    SmtpSettings.cs
Program.cs
appsettings.json
HustleTemply.http
```

Removed (legacy): `GoogleController`, `GoogleServices`, `WeatherForecast*`.

---

## Program.cs setup

```csharp
builder.Services.Configure<GoogleSheetsSettings>(...);
builder.Services.Configure<SheetCopySettings>(...);
builder.Services.Configure<SmtpSettings>(...);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IGoogleSheetService, GoogleSheetService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ISheetExportService, SheetExportService>();
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
```

---

## Requirements checklist

| Requirement | Status |
|-------------|--------|
| Nullable fields → null when empty | Done |
| Boolean parsing True/False, default false | Done |
| Decimal empty → null | Done |
| Unknown sections skipped | Done |
| XML docs on public API methods | Done |
| `ILogger<T>` for fetch/parse | Done |
| Product + Product Image join | Done |
| Dual credential (file + embedded JSON) | Done |
| Read-only Sheets scope | Done |

---

## Local development

1. Set `GoogleSheets:SpreadsheetId` in `appsettings.json` or user secrets.
2. Ensure credentials (file or `ServiceAccountJson`).
3. Share the sheet with the service account `client_email`.
4. `dotnet run` → `http://localhost:5127` (see `Properties/launchSettings.json`).
5. Test with `HustleTemply.http` or `GET /api/sheet-data`.

---

## Out of scope

- Gmail API / domain-wide delegation (replaced by SMTP App Password)
- Gmail **receive** webhook (Push notifications / Pub/Sub)
- Standalone `POST /api/email/send`
