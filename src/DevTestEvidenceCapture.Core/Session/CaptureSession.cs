namespace DevTestEvidenceCapture.Core.Session;

public enum CaptureSessionStatus
{
    Active,
    Stopped,
    Interrupted,
}

public sealed class CaptureSession
{
    public required string TicketNumber { get; init; }
    public required string ScenarioName { get; init; }
    public required string SanitizedTicketFolderName { get; init; }
    public required string SanitizedScenarioFolderName { get; init; }
    public required string FolderPath { get; init; }
    public CaptureSessionStatus Status { get; set; } = CaptureSessionStatus.Active;
    public DateTimeOffset StartTimestamp { get; init; }
    public DateTimeOffset? EndTimestamp { get; set; }
    public List<Screenshot> Screenshots { get; } = new();
}
