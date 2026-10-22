using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Tests.Metadata;

public class ScenarioMetadataServiceTests : IDisposable
{
    private readonly string _sessionFolder = Path.Combine(Path.GetTempPath(), $"dtec-metadata-{Guid.NewGuid():N}");

    public ScenarioMetadataServiceTests()
    {
        Directory.CreateDirectory(_sessionFolder);
    }

    [Fact]
    public async Task WriteAsync_ThenReadAsync_RoundTripsSessionFields()
    {
        var sut = new ScenarioMetadataService();
        var session = new CaptureSession
        {
            TicketNumber = "TICKET-123",
            ScenarioName = "Login flow",
            SanitizedTicketFolderName = "TICKET-123",
            SanitizedScenarioFolderName = "Login flow",
            FolderPath = _sessionFolder,
            StartTimestamp = DateTimeOffset.Parse("2026-09-10T10:00:00Z"),
            EndTimestamp = DateTimeOffset.Parse("2026-09-10T10:05:00Z"),
        };
        session.Screenshots.Add(new Screenshot
        {
            SequenceNumber = 1,
            OriginalFileName = "01.png",
            FinalFileName = "01_Login_page.png",
            CaptureTimestamp = DateTimeOffset.Parse("2026-09-10T10:01:00Z"),
        });

        await sut.WriteAsync(session);
        var metadata = await sut.ReadAsync(_sessionFolder);

        Assert.Equal("TICKET-123", metadata.TicketNumber);
        Assert.Equal("Login flow", metadata.ScenarioName);
        Assert.Equal(session.StartTimestamp, metadata.SessionStart);
        Assert.Equal(session.EndTimestamp, metadata.SessionEnd);
        Assert.Equal(1, metadata.ScreenshotCount);
        Assert.Equal("01_Login_page.png", metadata.Screenshots.Single().FileName);
    }

    [Fact]
    public async Task WriteAsync_WithoutEndTimestamp_PersistsNullSessionEnd()
    {
        var sut = new ScenarioMetadataService();
        var session = new CaptureSession
        {
            TicketNumber = "TICKET-1",
            ScenarioName = "Scenario",
            SanitizedTicketFolderName = "TICKET-1",
            SanitizedScenarioFolderName = "Scenario",
            FolderPath = _sessionFolder,
            StartTimestamp = DateTimeOffset.Now,
        };

        await sut.WriteAsync(session);
        var metadata = await sut.ReadAsync(_sessionFolder);

        Assert.Null(metadata.SessionEnd);
        Assert.Equal(0, metadata.ScreenshotCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sessionFolder))
        {
            Directory.Delete(_sessionFolder, recursive: true);
        }
    }
}
