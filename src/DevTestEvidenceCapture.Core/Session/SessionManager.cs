using DevTestEvidenceCapture.Core.Capture;
using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Storage;

namespace DevTestEvidenceCapture.Core.Session;

public sealed class SessionManager : ISessionManager
{
    private readonly IEvidenceStorage _storage;
    private readonly IScreenCaptureService _captureService;
    private readonly IScenarioMetadataService _metadataService;
    private readonly INamingService _namingService;
    private readonly IAppConfigurationService _configService;
    private readonly IRegionSelector? _regionSelector;
    private bool _isCaptureInProgress;

    /// <param name="regionSelector">
    /// When provided, each capture lets the user pick (and annotate) the area to save; when
    /// <c>null</c>, the whole primary monitor is saved.
    /// </param>
    public SessionManager(
        IEvidenceStorage storage,
        IScreenCaptureService captureService,
        IScenarioMetadataService metadataService,
        INamingService namingService,
        IAppConfigurationService configService,
        IRegionSelector? regionSelector = null)
    {
        _storage = storage;
        _captureService = captureService;
        _metadataService = metadataService;
        _namingService = namingService;
        _configService = configService;
        _regionSelector = regionSelector;
    }

    public CaptureSession? Current { get; private set; }

    public event EventHandler<CaptureFailedEventArgs>? CaptureFailed;

    public async Task StartAsync(string ticketNumber, string scenarioName)
    {
        if (Current is { Status: CaptureSessionStatus.Active })
        {
            throw new InvalidOperationException("A capture session is already active.");
        }

        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            throw new ArgumentException("Ticket number is required.", nameof(ticketNumber));
        }

        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            throw new ArgumentException("Scenario name is required.", nameof(scenarioName));
        }

        var config = await _configService.LoadAsync();
        if (string.IsNullOrWhiteSpace(config.RootFolderPath) || !Directory.Exists(config.RootFolderPath))
        {
            throw new InvalidOperationException("No valid, writable root evidence folder is configured.");
        }

        Current = _storage.CreateSessionFolder(config.RootFolderPath, ticketNumber, scenarioName);

        config.LastActiveSessionPath = Current.FolderPath;
        config.RecentTicketNumbers = RecentTicketNumbers.Add(config.RecentTicketNumbers, ticketNumber);
        await _configService.SaveAsync(config);
        await _metadataService.WriteAsync(Current);
    }

    /// <remarks>
    /// The returned session stays <see cref="CaptureSessionStatus.Interrupted"/> (so the capture
    /// shortcut does nothing) until the user calls <see cref="AcceptResume"/>.
    /// </remarks>
    public async Task<CaptureSession?> TryResumeInterruptedSessionAsync()
    {
        var config = await _configService.LoadAsync();
        var interrupted = await _storage.TryFindInterruptedSessionAsync(config);
        if (interrupted is null)
        {
            return null;
        }

        Current = interrupted;
        return interrupted;
    }

    public void AcceptResume()
    {
        if (Current is { Status: CaptureSessionStatus.Interrupted })
        {
            Current.Status = CaptureSessionStatus.Active;
        }
    }

    public async Task DeclineResumeAsync()
    {
        if (Current is { Status: CaptureSessionStatus.Interrupted })
        {
            Current = null;
        }

        // Forget the interrupted session so the user isn't asked again on every launch.
        var config = await _configService.LoadAsync();
        config.LastActiveSessionPath = null;
        await _configService.SaveAsync(config);
    }

    public async Task<Screenshot?> CaptureAsync()
    {
        // A second hotkey press while the region selector is still open is ignored.
        if (_isCaptureInProgress || Current is not { Status: CaptureSessionStatus.Active } session)
        {
            return null;
        }

        _isCaptureInProgress = true;
        try
        {
            var pngBytes = await _captureService.CapturePrimaryMonitorPngAsync();
            string? note = null;

            if (_regionSelector is not null)
            {
                var selection = await _regionSelector.SelectRegionAsync(pngBytes);
                if (selection is null || session.Status != CaptureSessionStatus.Active)
                {
                    return null;
                }

                pngBytes = CaptureImageRenderer.Render(pngBytes, selection.Region, selection.Annotations);
                note = string.IsNullOrWhiteSpace(selection.Note) ? null : selection.Note.Trim();
            }

            var sequenceNumber = _storage.GetNextSequenceNumber(session.FolderPath);
            var fileName = _namingService.FormatScreenshotFileName(sequenceNumber);
            var filePath = Path.Combine(session.FolderPath, fileName);

            // Never overwrite existing evidence (FR-019).
            await AtomicFile.WriteAllBytesAsync(filePath, pngBytes, overwrite: false);

            var screenshot = new Screenshot
            {
                SequenceNumber = sequenceNumber,
                OriginalFileName = fileName,
                FinalFileName = fileName,
                CaptureTimestamp = DateTimeOffset.Now,
                Note = note,
            };
            session.Screenshots.Add(screenshot);
            await _metadataService.WriteAsync(session);

            return screenshot;
        }
        catch (Exception ex)
        {
            CaptureFailed?.Invoke(this, new CaptureFailedEventArgs { Exception = ex });
            return null;
        }
        finally
        {
            _isCaptureInProgress = false;
        }
    }

    public async Task StopAsync()
    {
        if (Current is null)
        {
            return;
        }

        Current.Status = CaptureSessionStatus.Stopped;
        Current.EndTimestamp = DateTimeOffset.Now;
        await _metadataService.WriteAsync(Current);

        var config = await _configService.LoadAsync();
        config.LastActiveSessionPath = null;
        await _configService.SaveAsync(config);
    }

    public async Task<RenameResult> RenameAsync(IReadOnlyDictionary<int, string?> sequenceNumberToMeaningfulName)
    {
        if (Current is null)
        {
            throw new InvalidOperationException("No session is available to rename.");
        }

        var result = _namingService.ApplyRenames(Current, sequenceNumberToMeaningfulName);
        await _metadataService.WriteAsync(Current);
        return result;
    }

    public Task SkipRenameAsync() => Task.CompletedTask;
}
