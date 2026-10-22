using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Session;

public class SessionManagerEdgeCaseTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-edgecases-{Guid.NewGuid():N}");
    private readonly ScenarioMetadataService _metadataService = new();
    private readonly SessionManager _sut;

    public SessionManagerEdgeCaseTests()
    {
        Directory.CreateDirectory(_rootFolder);
        var namingService = new NamingService();
        var storage = new EvidenceStorage(namingService, _metadataService);
        var configService = new FakeAppConfigurationService { Config = new AppConfiguration { RootFolderPath = _rootFolder } };
        _sut = new SessionManager(storage, new FakeScreenCaptureService(), _metadataService, namingService, configService);
    }

    [Fact]
    public async Task StopAsync_WithZeroScreenshotsCaptured_ProducesEmptyScenarioJson()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        var folderPath = _sut.Current!.FolderPath;

        await _sut.StopAsync();

        var metadata = await _metadataService.ReadAsync(folderPath);
        Assert.Equal(0, metadata.ScreenshotCount);
        Assert.Empty(metadata.Screenshots);
        Assert.NotNull(metadata.SessionEnd);
    }

    [Theory]
    [InlineData("", "Scenario")]
    [InlineData("   ", "Scenario")]
    [InlineData("TICKET-1", "")]
    [InlineData("TICKET-1", "   ")]
    public async Task StartAsync_WithMissingTicketOrScenario_Throws(string ticketNumber, string scenarioName)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.StartAsync(ticketNumber, scenarioName));
    }

    [Fact]
    public async Task StartAsync_WithMissingOrUnwritableRootFolder_Throws()
    {
        var namingService = new NamingService();
        var storage = new EvidenceStorage(namingService, _metadataService);
        var configService = new FakeAppConfigurationService
        {
            Config = new AppConfiguration { RootFolderPath = Path.Combine(_rootFolder, "does-not-exist") },
        };
        var sut = new SessionManager(storage, new FakeScreenCaptureService(), _metadataService, namingService, configService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartAsync("TICKET-1", "Scenario"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
