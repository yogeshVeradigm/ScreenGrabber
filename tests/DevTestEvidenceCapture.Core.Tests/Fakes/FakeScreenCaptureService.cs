using DevTestEvidenceCapture.Core.Capture;

namespace DevTestEvidenceCapture.Core.Tests.Fakes;

public sealed class FakeScreenCaptureService : IScreenCaptureService
{
    public byte[] BytesToReturn { get; set; } = { 1, 2, 3 };
    public bool ShouldThrow { get; set; }
    public int CallCount { get; private set; }

    public Task<byte[]> CapturePrimaryMonitorPngAsync()
    {
        CallCount++;
        if (ShouldThrow)
        {
            throw new IOException("Simulated capture failure.");
        }

        return Task.FromResult(BytesToReturn);
    }
}
