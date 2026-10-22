using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Storage;

public class EvidenceStorageResumeTests : IDisposable
{
    private readonly string _sessionFolder = Path.Combine(Path.GetTempPath(), $"dtec-resume-{Guid.NewGuid():N}");
    private readonly FakeScenarioMetadataService _metadataService = new();
    private readonly EvidenceStorage _sut;

    public EvidenceStorageResumeTests()
    {
        _sut = new EvidenceStorage(new NamingService(), _metadataService);
        Directory.CreateDirectory(_sessionFolder);
    }

    [Fact]
    public async Task TryFindInterruptedSessionAsync_WithNoPriorSession_ReturnsNull()
    {
        var config = new AppConfiguration { LastActiveSessionPath = null };

        var result = await _sut.TryFindInterruptedSessionAsync(config);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryFindInterruptedSessionAsync_WithInterruptedSession_ReturnsItAsInterrupted()
    {
        _metadataService.MetadataByFolderPath[_sessionFolder] = new ScenarioMetadata
        {
            TicketNumber = "TICKET-1",
            ScenarioName = "Scenario",
            SessionStart = DateTimeOffset.Now,
            SessionEnd = null,
            ScreenshotCount = 1,
            Screenshots = new List<ScenarioScreenshotMetadata>
            {
                new() { SequenceNumber = 1, FileName = "01.png", CapturedAt = DateTimeOffset.Now },
            },
        };
        var config = new AppConfiguration { LastActiveSessionPath = _sessionFolder };

        var result = await _sut.TryFindInterruptedSessionAsync(config);

        Assert.NotNull(result);
        Assert.Equal(CaptureSessionStatus.Interrupted, result!.Status);
        Assert.Equal("TICKET-1", result.TicketNumber);
        Assert.Single(result.Screenshots);
    }

    [Fact]
    public async Task TryFindInterruptedSessionAsync_WithCompletedSession_ReturnsNull()
    {
        _metadataService.MetadataByFolderPath[_sessionFolder] = new ScenarioMetadata
        {
            TicketNumber = "TICKET-1",
            ScenarioName = "Scenario",
            SessionStart = DateTimeOffset.Now,
            SessionEnd = DateTimeOffset.Now,
            ScreenshotCount = 0,
            Screenshots = new List<ScenarioScreenshotMetadata>(),
        };
        var config = new AppConfiguration { LastActiveSessionPath = _sessionFolder };

        var result = await _sut.TryFindInterruptedSessionAsync(config);

        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sessionFolder))
        {
            Directory.Delete(_sessionFolder, recursive: true);
        }
    }
}
