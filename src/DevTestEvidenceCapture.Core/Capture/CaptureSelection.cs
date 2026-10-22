namespace DevTestEvidenceCapture.Core.Capture;

/// <summary>The area the user chose to save, anything they drew on it, and an optional note.</summary>
public sealed record CaptureSelection(CaptureRegion Region, IReadOnlyList<Annotation> Annotations, string? Note = null);
