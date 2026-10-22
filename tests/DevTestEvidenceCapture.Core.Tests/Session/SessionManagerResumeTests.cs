using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Session;

public class SessionManagerResumeTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-resumemgr-{Guid.NewGuid():N}");
    private readonly FakeAppConfigurationService _configService;
    private readonly NamingService _namingService = new();
    private readonly ScenarioMetadataService _metadataService = new();
    private readonly EvidenceStorage _storage;

    public SessionManagerResumeTests()
    {
        Directory.CreateDirectory(_rootFolder);
        _storage = new EvidenceStorage(_namingService, _metadataService);
        _configService = new FakeAppConfigurationService { Config = new AppConfiguration { RootFolderPath = _rootFolder } };
    }

    private SessionManager CreateSut() =>
        new(_storage, new FakeScreenCaptureService(), _metadataService, _namingService, _configService);

    // Simulates the app being killed mid-session: the session is started and captured into, but
    // never stopped, then a fresh SessionManager is created as on the next launch.
    private async Task<SessionManager> SimulateRelaunchAfterCrashAsync()
    {
        var before = CreateSut();
        await before.StartAsync("CAMP-1", "Scenario");
        await before.CaptureAsync();
        return CreateSut();
    }

    [Fact]
    public async Task TryResume_FoundSession_IsNotCapturableUntilAccepted()
    {
        var sut = await SimulateRelaunchAfterCrashAsync();

        var resumed = await sut.TryResumeInterruptedSessionAsync();
        var whilePrompting = await sut.CaptureAsync();

        Assert.NotNull(resumed);
        Assert.Equal(CaptureSessionStatus.Interrupted, resumed!.Status);
        Assert.Null(whilePrompting);
        Assert.Single(resumed.Screenshots);
    }

    [Fact]
    public async Task AcceptResume_ContinuesNumberingFromDisk()
    {
        var sut = await SimulateRelaunchAfterCrashAsync();
        await sut.TryResumeInterruptedSessionAsync();

        sut.AcceptResume();
        var next = await sut.CaptureAsync();

        Assert.Equal(CaptureSessionStatus.Active, sut.Current!.Status);
        Assert.Equal(2, next!.SequenceNumber);
    }

    [Fact]
    public async Task DeclineResume_ForgetsTheSessionSoItIsNotOfferedAgain()
    {
        var sut = await SimulateRelaunchAfterCrashAsync();
        await sut.TryResumeInterruptedSessionAsync();

        await sut.DeclineResumeAsync();

        Assert.Null(sut.Current);
        Assert.Null(_configService.Config.LastActiveSessionPath);
        Assert.Null(await CreateSut().TryResumeInterruptedSessionAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
