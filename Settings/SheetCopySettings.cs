namespace HustleTemply.Settings;

public class SheetCopySettings
{
    public const string SectionName = "SheetCopy";

    public string NamePrefix { get; set; } = "Copy - ";

    public string ShareRole { get; set; } = "writer";
}
