using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Integration;

public class ScenarioMetadataRenameIntegrationTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-integration-rename-{Guid.NewGuid():N}");

    [Fact]
    public async Task FullSessionWithRenaming_ScenarioJsonReflectsFinalNamesNotOriginals()
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

        await sessionManager.RenameAsync(new Dictionary<int, string?> { [1] = "Login page", [2] = null });

        var scenarioJsonPath = Path.Combine(folderPath, "scenario.json");
        SchemaValidationHelper.AssertValidScenarioMetadata(scenarioJsonPath);

        var metadata = await metadataService.ReadAsync(folderPath);
        var first = metadata.Screenshots.Single(s => s.SequenceNumber == 1);
        var second = metadata.Screenshots.Single(s => s.SequenceNumber == 2);
        Assert.NotEqual("01.png", first.FileName);
        Assert.StartsWith("01_", first.FileName);
        Assert.Equal("02.png", second.FileName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
