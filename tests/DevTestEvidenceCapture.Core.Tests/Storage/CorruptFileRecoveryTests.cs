using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Storage;

namespace DevTestEvidenceCapture.Core.Tests.Storage;

/// <summary>
/// A crash mid-write used to leave empty or partial JSON files that then crashed the app on launch.
/// </summary>
public class CorruptFileRecoveryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"dtec-corrupt-{Guid.NewGuid():N}");

    public CorruptFileRecoveryTests()
    {
        Directory.CreateDirectory(_folder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ \"RootFolderPath\": \"C:\\\\Evid")]
    public async Task LoadAsync_WithCorruptConfig_ReturnsDefaults(string content)
    {
        var path = Path.Combine(_folder, "config.json");
        await File.WriteAllTextAsync(path, content);

        var config = await new AppConfigurationService(path).LoadAsync();

        Assert.Null(config.RootFolderPath);
        Assert.Equal("Ctrl+Shift+S", config.CaptureShortcut);
    }

    [Fact]
    public async Task ReadAsync_WithCorruptScenarioJson_ThrowsInvalidDataException()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "scenario.json"), "{ \"ticketNumber\": ");

        await Assert.ThrowsAsync<InvalidDataException>(() => new ScenarioMetadataService().ReadAsync(_folder));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ \"ticketNumber\": \"CAMP-1\", ")]
    public async Task TryFindInterruptedSessionAsync_WithCorruptScenarioJson_ReturnsNull(string content)
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "scenario.json"), content);
        var storage = new EvidenceStorage(new NamingService(), new ScenarioMetadataService());

        var result = await storage.TryFindInterruptedSessionAsync(new AppConfiguration { LastActiveSessionPath = _folder });

        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
