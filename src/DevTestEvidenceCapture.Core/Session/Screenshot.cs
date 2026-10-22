namespace DevTestEvidenceCapture.Core.Session;

public sealed class Screenshot
{
    public required int SequenceNumber { get; init; }
    public required string OriginalFileName { get; init; }
    public required string FinalFileName { get; set; }
    public required DateTimeOffset CaptureTimestamp { get; init; }

    /// <summary>Optional note the user typed when capturing, e.g. what was done or observed.</summary>
    public string? Note { get; init; }
}
