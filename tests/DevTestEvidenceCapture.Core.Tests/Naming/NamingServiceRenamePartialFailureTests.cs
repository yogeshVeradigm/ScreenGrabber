using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Tests.Naming;

public class NamingServiceRenamePartialFailureTests : IDisposable
{
    private readonly string _sessionFolder = Path.Combine(Path.GetTempPath(), $"dtec-rename-fail-{Guid.NewGuid():N}");
    private readonly NamingService _sut = new();

    public NamingServiceRenamePartialFailureTests()
    {
        Directory.CreateDirectory(_sessionFolder);
    }

    [Fact]
    public void ApplyRenames_WhenAFileMoveFailsMidBatch_KeepsAlreadyRenamedFilesAndLeavesRestUntouched()
    {
        var session = new CaptureSession
        {
            TicketNumber = "TICKET-1",
            ScenarioName = "Scenario",
            SanitizedTicketFolderName = "TICKET-1",
            SanitizedScenarioFolderName = "Scenario",
            FolderPath = _sessionFolder,
            StartTimestamp = DateTimeOffset.Now,
        };
        File.WriteAllText(Path.Combine(_sessionFolder, "01.png"), string.Empty);
        File.WriteAllText(Path.Combine(_sessionFolder, "02.png"), string.Empty);
        session.Screenshots.Add(new Screenshot { SequenceNumber = 1, OriginalFileName = "01.png", FinalFileName = "01.png", CaptureTimestamp = DateTimeOffset.Now });
        session.Screenshots.Add(new Screenshot { SequenceNumber = 2, OriginalFileName = "02.png", FinalFileName = "02.png", CaptureTimestamp = DateTimeOffset.Now });

        // Force the second rename to fail: File.Move throws when the destination already
        // exists as a directory.
        Directory.CreateDirectory(Path.Combine(_sessionFolder, "02_Second.png"));

        var result = _sut.ApplyRenames(session, new Dictionary<int, string?> { [1] = "First", [2] = "Second" });

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { 1 }, result.SuccessfullyRenamedSequenceNumbers);
        Assert.NotNull(result.Error);

        Assert.Equal("01_First.png", session.Screenshots[0].FinalFileName);
        Assert.True(File.Exists(Path.Combine(_sessionFolder, "01_First.png")));

        Assert.Equal("02.png", session.Screenshots[1].FinalFileName);
        Assert.True(File.Exists(Path.Combine(_sessionFolder, "02.png")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_sessionFolder))
        {
            Directory.Delete(_sessionFolder, recursive: true);
        }
    }
}
