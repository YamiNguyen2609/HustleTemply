namespace HustleTemply.Models;

public record CopyAndEmailResponse(
    string SourceSpreadsheetId,
    string CopiedSpreadsheetId,
    string RecipientEmail,
    bool EmailSent);
