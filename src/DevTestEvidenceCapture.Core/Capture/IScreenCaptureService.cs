namespace DevTestEvidenceCapture.Core.Capture;

public interface IScreenCaptureService
{
    Task<byte[]> CapturePrimaryMonitorPngAsync();
}
