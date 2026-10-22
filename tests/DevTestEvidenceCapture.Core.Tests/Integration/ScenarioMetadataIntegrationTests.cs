using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Integration;

public class ScenarioMetadataIntegrationTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-integration-{Guid.NewGuid():N}");

    [Fact]
    public async Task FullSessionWithoutRenaming_ProducesSchemaValidScenarioJsonMatchingDisk()
    {
        Directory.CreateDirectory(_rootFolder);
        var namingService = new NamingService();
        var metadataService = new ScenarioMetadataService();
        var storage = new EvidenceStorage(namingService, metadataService);
        var configService = new FakeAppConfigurationService { Config = new AppConfiguration { RootFolderPath = _rootFolder } };
        var sessionManager = new SessionManager(storage, new FakeScreenCaptureService(), metadataService, namingService, configService);

        await sessionManager.StartAsync("TICKET-1", "Scenario");
        await sessionManager.CaptureAsync();
        await sessionManager.CaptureAsync();
        var folderPath = sessionManager.Current!.FolderPath;
        await sessionManager.StopAsync();

        var scenarioJsonPath = Path.Combine(folderPath, "scenario.json");
        SchemaValidationHelper.AssertValidScenarioMetadata(scenarioJsonPath);

        var metadata = await metadataService.ReadAsync(folderPath);
        Assert.Equal(2, metadata.ScreenshotCount);
        Assert.NotNull(metadata.SessionEnd);
        foreach (var screenshot in metadata.Screenshots)
        {
            Assert.True(File.Exists(Path.Combine(folderPath, screenshot.FileName)));
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
