using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Session;

public class SessionManagerRenameTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-sessionrename-{Guid.NewGuid():N}");
    private readonly ScenarioMetadataService _metadataService = new();
    private readonly SessionManager _sut;

    public SessionManagerRenameTests()
    {
        Directory.CreateDirectory(_rootFolder);
        var namingService = new NamingService();
        var storage = new EvidenceStorage(namingService, _metadataService);
        var configService = new FakeAppConfigurationService { Config = new AppConfiguration { RootFolderPath = _rootFolder } };
        _sut = new SessionManager(storage, new FakeScreenCaptureService(), _metadataService, namingService, configService);
    }

    [Fact]
    public async Task RenameAsync_UpdatesScenarioJsonToFinalFileNames()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        await _sut.CaptureAsync();
        await _sut.CaptureAsync();
        var folderPath = _sut.Current!.FolderPath;
        await _sut.StopAsync();

        await _sut.RenameAsync(new Dictionary<int, string?> { [1] = "Login page", [2] = null });

        var metadata = await _metadataService.ReadAsync(folderPath);
        Assert.Equal("01_Login page.png", metadata.Screenshots.Single(s => s.SequenceNumber == 1).FileName);
        Assert.Equal("02.png", metadata.Screenshots.Single(s => s.SequenceNumber == 2).FileName);
    }

    [Fact]
    public async Task SkipRenameAsync_LeavesScenarioJsonWithOriginalFileNames()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        await _sut.CaptureAsync();
        var folderPath = _sut.Current!.FolderPath;
        await _sut.StopAsync();

        await _sut.SkipRenameAsync();

        var metadata = await _metadataService.ReadAsync(folderPath);
        Assert.Equal("01.png", metadata.Screenshots.Single().FileName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
