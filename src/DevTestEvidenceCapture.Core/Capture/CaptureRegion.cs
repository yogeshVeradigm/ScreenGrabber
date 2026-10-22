namespace DevTestEvidenceCapture.Core.Capture;

/// <summary>A rectangle, in physical pixels of the captured image, selected for saving.</summary>
public readonly record struct CaptureRegion(int X, int Y, int Width, int Height);
