using DevTestEvidenceCapture.Core.Capture;

namespace DevTestEvidenceCapture.Core.Tests.Fakes;

public sealed class FakeRegionSelector : IRegionSelector
{
    public CaptureSelection? SelectionToReturn { get; set; }

    /// <summary>When set, selection doesn't complete until this task source is completed.</summary>
    public TaskCompletionSource<CaptureSelection?>? PendingSelection { get; set; }

    public int CallCount { get; private set; }

    public static CaptureSelection Region(int x, int y, int width, int height, params Annotation[] annotations) =>
        new(new CaptureRegion(x, y, width, height), annotations);

    public Task<CaptureSelection?> SelectRegionAsync(byte[] screenPng)
    {
        CallCount++;
        return PendingSelection?.Task ?? Task.FromResult(SelectionToReturn);
    }
}
