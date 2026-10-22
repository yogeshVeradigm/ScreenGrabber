using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Session;

public class SessionManagerCaptureFailureTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-capturefail-{Guid.NewGuid():N}");
    private readonly FakeScreenCaptureService _captureService = new();
    private readonly SessionManager _sut;

    public SessionManagerCaptureFailureTests()
    {
        Directory.CreateDirectory(_rootFolder);
        var namingService = new NamingService();
        var metadataService = new ScenarioMetadataService();
        var storage = new EvidenceStorage(namingService, metadataService);
        var configService = new FakeAppConfigurationService { Config = new AppConfiguration { RootFolderPath = _rootFolder } };
        _sut = new SessionManager(storage, _captureService, metadataService, namingService, configService);
    }

    [Fact]
    public async Task CaptureAsync_WhenCaptureServiceThrows_KeepsSessionActiveAndRaisesFailureEvent()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _captureService.ShouldThrow = true;

        Exception? raisedException = null;
        _sut.CaptureFailed += (_, args) => raisedException = args.Exception;

        var result = await _sut.CaptureAsync();

        Assert.Null(result);
        Assert.NotNull(raisedException);
        Assert.Equal(CaptureSessionStatus.Active, _sut.Current!.Status);
    }

    [Fact]
    public async Task CaptureAsync_AfterAFailure_StillAllowsSubsequentSuccessfulCapture()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _captureService.ShouldThrow = true;
        await _sut.CaptureAsync();

        _captureService.ShouldThrow = false;
        var result = await _sut.CaptureAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result!.SequenceNumber);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
