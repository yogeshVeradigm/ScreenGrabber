namespace DevTestEvidenceCapture.Core.Capture;

public interface IRegionSelector
{
    /// <summary>
    /// Lets the user choose the part of <paramref name="screenPng"/> to keep and optionally annotate it.
    /// Returns <c>null</c> if the user cancels.
    /// </summary>
    Task<CaptureSelection?> SelectRegionAsync(byte[] screenPng);
}
