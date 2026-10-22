using System.Drawing;
using DevTestEvidenceCapture.Core.Capture;
using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Capture;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Session;

public class SessionManagerRegionCaptureTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-region-{Guid.NewGuid():N}");
    private readonly FakeScreenCaptureService _captureService = new() { BytesToReturn = TestPngFactory.CreateSplitPng(40, 20) };
    private readonly FakeRegionSelector _regionSelector = new();
    private readonly FakeAppConfigurationService _configService;
    private readonly SessionManager _sut;

    public SessionManagerRegionCaptureTests()
    {
        Directory.CreateDirectory(_rootFolder);
        var namingService = new NamingService();
        var metadataService = new ScenarioMetadataService();
        var storage = new EvidenceStorage(namingService, metadataService);
        _configService = new FakeAppConfigurationService { Config = new AppConfiguration { RootFolderPath = _rootFolder } };
        _sut = new SessionManager(storage, _captureService, metadataService, namingService, _configService, _regionSelector);
    }

    [Fact]
    public async Task CaptureAsync_WithSelectedRegion_SavesOnlyThatRegion()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.SelectionToReturn = FakeRegionSelector.Region(2, 3, 10, 8);

        var result = await _sut.CaptureAsync();

        Assert.NotNull(result);
        using var saved = TestPngFactory.Decode(File.ReadAllBytes(Path.Combine(_sut.Current!.FolderPath, result!.FinalFileName)));
        Assert.Equal(10, saved.Width);
        Assert.Equal(8, saved.Height);
        Assert.Equal(Color.Red.ToArgb(), saved.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public async Task CaptureAsync_WithAnnotations_BurnsThemIntoTheSavedImage()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        var black = Color.Black.ToArgb();
        var box = new ShapeAnnotation(ShapeKind.Rectangle, new RectangleF(4, 4, 6, 6), 1, Filled: true, black);
        _regionSelector.SelectionToReturn = FakeRegionSelector.Region(2, 2, 16, 16, box);

        var result = await _sut.CaptureAsync();

        using var saved = TestPngFactory.Decode(File.ReadAllBytes(Path.Combine(_sut.Current!.FolderPath, result!.FinalFileName)));
        Assert.Equal(black, saved.GetPixel(5, 5).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), saved.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public async Task CaptureAsync_WithNote_SavesTrimmedNoteToSchemaValidScenarioJson()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.SelectionToReturn = FakeRegionSelector.Region(0, 0, 10, 10) with { Note = "  Clicked Save, got validation error  " };

        var result = await _sut.CaptureAsync();

        Assert.Equal("Clicked Save, got validation error", result!.Note);
        var folderPath = _sut.Current!.FolderPath;
        Integration.SchemaValidationHelper.AssertValidScenarioMetadata(Path.Combine(folderPath, "scenario.json"));
        var metadata = await new ScenarioMetadataService().ReadAsync(folderPath);
        Assert.Equal("Clicked Save, got validation error", metadata.Screenshots.Single().Note);
    }

    [Fact]
    public async Task CaptureAsync_WithBlankNote_OmitsNoteFromScenarioJson()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.SelectionToReturn = FakeRegionSelector.Region(0, 0, 10, 10) with { Note = "   " };

        var result = await _sut.CaptureAsync();

        Assert.Null(result!.Note);
        var json = await File.ReadAllTextAsync(Path.Combine(_sut.Current!.FolderPath, "scenario.json"));
        Assert.DoesNotContain("\"note\"", json);
    }

    [Fact]
    public async Task ResumedSession_KeepsNotesFromBeforeTheCrash()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.SelectionToReturn = FakeRegionSelector.Region(0, 0, 10, 10) with { Note = "Before crash" };
        await _sut.CaptureAsync();

        var namingService = new NamingService();
        var metadataService = new ScenarioMetadataService();
        var relaunched = new SessionManager(
            new EvidenceStorage(namingService, metadataService), _captureService, metadataService, namingService, _configService);
        var resumed = await relaunched.TryResumeInterruptedSessionAsync();

        Assert.Equal("Before crash", resumed!.Screenshots.Single().Note);
    }

    [Fact]
    public async Task CaptureAsync_WhenSelectionCancelled_SavesNothingAndRaisesNoFailure()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.SelectionToReturn = null;
        var failureRaised = false;
        _sut.CaptureFailed += (_, _) => failureRaised = true;

        var result = await _sut.CaptureAsync();

        Assert.Null(result);
        Assert.False(failureRaised);
        Assert.Empty(_sut.Current!.Screenshots);
        Assert.Empty(Directory.EnumerateFiles(_sut.Current.FolderPath, "*.png"));
    }

    [Fact]
    public async Task CaptureAsync_AfterCancelledSelection_NextCaptureStillStartsAtOne()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.SelectionToReturn = null;
        await _sut.CaptureAsync();

        _regionSelector.SelectionToReturn = FakeRegionSelector.Region(0, 0, 40, 20);
        var result = await _sut.CaptureAsync();

        Assert.Equal(1, result!.SequenceNumber);
    }

    [Fact]
    public async Task CaptureAsync_WhileSelectionIsOpen_IgnoresSecondPress()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.PendingSelection = new TaskCompletionSource<CaptureSelection?>();

        var firstCapture = _sut.CaptureAsync();
        var secondResult = await _sut.CaptureAsync();
        _regionSelector.PendingSelection.SetResult(FakeRegionSelector.Region(0, 0, 10, 10));
        var firstResult = await firstCapture;

        Assert.Null(secondResult);
        Assert.Equal(1, _regionSelector.CallCount);
        Assert.NotNull(firstResult);
        Assert.Single(_sut.Current!.Screenshots);
    }

    [Fact]
    public async Task CaptureAsync_WhenSessionStoppedDuringSelection_SavesNothing()
    {
        await _sut.StartAsync("TICKET-1", "Scenario");
        _regionSelector.PendingSelection = new TaskCompletionSource<CaptureSelection?>();

        var capture = _sut.CaptureAsync();
        await _sut.StopAsync();
        _regionSelector.PendingSelection.SetResult(FakeRegionSelector.Region(0, 0, 10, 10));
        var result = await capture;

        Assert.Null(result);
        Assert.Empty(_sut.Current!.Screenshots);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
