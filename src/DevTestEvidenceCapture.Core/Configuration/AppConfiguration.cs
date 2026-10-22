namespace DevTestEvidenceCapture.Core.Configuration;

public sealed class AppConfiguration
{
    public string? RootFolderPath { get; set; }
    public string CaptureShortcut { get; set; } = "Ctrl+Shift+S";
    public string? LastActiveSessionPath { get; set; }
    public AnnotationSettings Annotation { get; set; } = new();
    public bool CopyToClipboard { get; set; } = true;

    /// <summary>Most recently used ticket numbers, newest first.</summary>
    public List<string> RecentTicketNumbers { get; set; } = new();
}
