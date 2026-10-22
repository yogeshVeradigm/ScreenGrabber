using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Tests.Naming;

public class NamingServiceRenameTests : IDisposable
{
    private readonly string _sessionFolder = Path.Combine(Path.GetTempPath(), $"dtec-rename-{Guid.NewGuid():N}");
    private readonly NamingService _sut = new();

    public NamingServiceRenameTests()
    {
        Directory.CreateDirectory(_sessionFolder);
    }

    private CaptureSession CreateSessionWithScreenshots(params int[] sequenceNumbers)
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

        foreach (var sequenceNumber in sequenceNumbers)
        {
            var fileName = _sut.FormatScreenshotFileName(sequenceNumber);
            File.WriteAllText(Path.Combine(_sessionFolder, fileName), string.Empty);
            session.Screenshots.Add(new Screenshot
            {
                SequenceNumber = sequenceNumber,
                OriginalFileName = fileName,
                FinalFileName = fileName,
                CaptureTimestamp = DateTimeOffset.Now,
            });
        }

        return session;
    }

    [Fact]
    public void ApplyRenames_PreservesNumericPrefix()
    {
        var session = CreateSessionWithScreenshots(1);

        var result = _sut.ApplyRenames(session, new Dictionary<int, string?> { [1] = "Login page" });

        Assert.True(result.Succeeded);
        Assert.Equal("01_Login page.png", session.Screenshots[0].FinalFileName);
        Assert.True(File.Exists(Path.Combine(_sessionFolder, "01_Login page.png")));
    }

    [Fact]
    public void ApplyRenames_BlankEntry_LeavesOriginalFileNameUnchanged()
    {
        var session = CreateSessionWithScreenshots(1, 2);

        var result = _sut.ApplyRenames(session, new Dictionary<int, string?> { [1] = "Login page", [2] = null });

        Assert.True(result.Succeeded);
        Assert.Equal("02.png", session.Screenshots[1].FinalFileName);
        Assert.True(File.Exists(Path.Combine(_sessionFolder, "02.png")));
    }

    [Fact]
    public void ApplyRenames_ConflictingMeaningfulNames_BlocksBatchAndTouchesNoFiles()
    {
        var session = CreateSessionWithScreenshots(1, 2);

        var result = _sut.ApplyRenames(session, new Dictionary<int, string?> { [1] = "Same name", [2] = "Same name" });

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { 1, 2 }, result.ConflictingSequenceNumbers.OrderBy(n => n));
        Assert.Equal("01.png", session.Screenshots[0].FinalFileName);
        Assert.Equal("02.png", session.Screenshots[1].FinalFileName);
        Assert.True(File.Exists(Path.Combine(_sessionFolder, "01.png")));
        Assert.True(File.Exists(Path.Combine(_sessionFolder, "02.png")));
    }

    [Fact]
    public void ApplyRenames_MeaningfulNameWithInvalidCharacters_IsSanitizedBeforeUse()
    {
        var session = CreateSessionWithScreenshots(1);

        var result = _sut.ApplyRenames(session, new Dictionary<int, string?> { [1] = "Search: results?" });

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(':', session.Screenshots[0].FinalFileName);
        Assert.DoesNotContain('?', session.Screenshots[0].FinalFileName);
        Assert.True(File.Exists(Path.Combine(_sessionFolder, session.Screenshots[0].FinalFileName)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_sessionFolder))
        {
            Directory.Delete(_sessionFolder, recursive: true);
        }
    }
}
