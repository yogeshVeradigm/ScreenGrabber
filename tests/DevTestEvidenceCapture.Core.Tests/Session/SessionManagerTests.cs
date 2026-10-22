using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Session;

public class SessionManagerTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-sessionmgr-{Guid.NewGuid():N}");
    private readonly FakeScreenCaptureService _captureService = new();
    private readonly SessionManager _sut;

    public SessionManagerTests()
    {
        Directory.CreateDirectory(_rootFolder);
        var namingService = new NamingService();
        var metadataService = new ScenarioMetadataService();
        var storage = new EvidenceStorage(namingService, metadataService);
        var configService = new FakeAppConfigurationService { Config = new AppConfiguration { RootFolderPath = _rootFolder } };
        _sut = new SessionManager(storage, _captureService, metadataService, namingService, configService);
    }

    [Fact]
    public async Task CaptureAsync_WithNoActiveSession_IsANoOpAndReturnsNull()
    {
        var result = await _sut.CaptureAsync();

        Assert.Null(result);
        Assert.Equal(0, _captureService.CallCount);
    }

    [Fact]
    public async Task CaptureAsync_CalledRepeatedly_IncrementsSequenceNumber()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");

        var first = await _sut.CaptureAsync();
        var second = await _sut.CaptureAsync();
        var third = await _sut.CaptureAsync();

        Assert.Equal(1, first!.SequenceNumber);
        Assert.Equal(2, second!.SequenceNumber);
        Assert.Equal(3, third!.SequenceNumber);
    }

    [Fact]
    public async Task StartAsync_WhileSessionAlreadyActive_Throws()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.StartAsync("TICKET-2", "Other"));
    }

    [Fact]
    public async Task StartAsync_AfterPriorSessionStopped_Succeeds()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        await _sut.StopAsync();

        await _sut.StartAsync("TICKET-2", "Other");

        Assert.Equal("TICKET-2", _sut.Current!.TicketNumber);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
