namespace HustleTemply.Settings;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; set; } = true;

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string AppPassword { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "HustleTemply";

    public string Subject { get; set; } = "Your HustleTemply sheet copy";
}
