using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Storage;

namespace DevTestEvidenceCapture.Core.Tests.Storage;

public class EvidenceStorageTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-root-{Guid.NewGuid():N}");
    private readonly EvidenceStorage _sut = new(new NamingService(), new ScenarioMetadataService());

    [Fact]
    public void CreateSessionFolder_CreatesNestedTicketAndScenarioFolders()
    {
        var session = _sut.CreateSessionFolder(_rootFolder, "TICKET-123", "Login flow");

        Assert.Equal(Path.Combine(_rootFolder, "TICKET-123", "Login flow"), session.FolderPath);
        Assert.True(Directory.Exists(session.FolderPath));
    }

    [Fact]
    public void CreateSessionFolder_SanitizesInvalidCharactersInTicketAndScenario()
    {
        var session = _sut.CreateSessionFolder(_rootFolder, "TICKET:123", "Search: results?");

        Assert.DoesNotContain(':', session.SanitizedTicketFolderName);
        Assert.DoesNotContain('?', session.SanitizedScenarioFolderName);
        Assert.True(Directory.Exists(session.FolderPath));
    }

    [Fact]
    public void CreateSessionFolder_WhenScenarioFolderAlreadyExists_CreatesDistinguishableFolder()
    {
        var first = _sut.CreateSessionFolder(_rootFolder, "TICKET-1", "Scenario");
        var second = _sut.CreateSessionFolder(_rootFolder, "TICKET-1", "Scenario");

        Assert.NotEqual(first.FolderPath, second.FolderPath);
        Assert.True(Directory.Exists(first.FolderPath));
        Assert.True(Directory.Exists(second.FolderPath));
    }

    [Fact]
    public void GetNextSequenceNumber_WhenFolderEmpty_ReturnsOne()
    {
        Directory.CreateDirectory(_rootFolder);

        Assert.Equal(1, _sut.GetNextSequenceNumber(_rootFolder));
    }

    [Fact]
    public void GetNextSequenceNumber_ScansExistingFilesIncludingRenamedAndWiderWidths()
    {
        Directory.CreateDirectory(_rootFolder);
        File.WriteAllText(Path.Combine(_rootFolder, "01_Login_page.png"), string.Empty);
        File.WriteAllText(Path.Combine(_rootFolder, "02.png"), string.Empty);
        File.WriteAllText(Path.Combine(_rootFolder, "100.png"), string.Empty);

        Assert.Equal(101, _sut.GetNextSequenceNumber(_rootFolder));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
