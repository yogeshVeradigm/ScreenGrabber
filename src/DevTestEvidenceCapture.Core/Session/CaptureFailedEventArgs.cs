namespace DevTestEvidenceCapture.Core.Session;

public sealed class CaptureFailedEventArgs : EventArgs
{
    public required Exception Exception { get; init; }
}
