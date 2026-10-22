using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DevTestEvidenceCapture.App.Capture;
using DevTestEvidenceCapture.App.Feedback;
using DevTestEvidenceCapture.App.Views;
using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Hotkey;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly ISessionManager _sessionManager;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly IAppConfigurationService _configService;
    private readonly CaptureFeedbackPresenter _feedbackPresenter;

    private string _ticketNumber = string.Empty;
    private string _scenarioName = string.Empty;
    private string? _rootFolderPath;
    private bool _isSessionActive;
    private string _statusText = "Idle";
    private int _screenshotCount;
    private string _captureShortcut = "Ctrl+Shift+S";
    private string? _warningText;
    private bool _copyToClipboard = true;
    private bool _isInitialized;

    public MainViewModel(ISessionManager sessionManager, IGlobalHotkeyService hotkeyService, IAppConfigurationService configService, CaptureFeedbackPresenter feedbackPresenter)
    {
        _sessionManager = sessionManager;
        _hotkeyService = hotkeyService;
        _configService = configService;
        _feedbackPresenter = feedbackPresenter;

        _hotkeyService.HotkeyPressed += async (_, _) => await OnHotkeyPressedAsync();
        _sessionManager.CaptureFailed += (_, args) => WarningText = $"Capture failed: {args.Exception.Message}";

        StartSessionCommand = new RelayCommand(StartSessionAsync, () => CanStart);
        StopSessionCommand = new RelayCommand(StopSessionAsync, () => IsSessionActive);
        BrowseRootFolderCommand = new RelayCommand(BrowseRootFolderAsync);
        ChangeShortcutCommand = new RelayCommand(ChangeShortcutAsync);
    }

    public event EventHandler<CaptureSession>? SessionStoppedWithScreenshots;

    public string TicketNumber
    {
        get => _ticketNumber;
        set
        {
            if (SetField(ref _ticketNumber, value))
            {
                RaiseCanStartChanged();
            }
        }
    }

    public ObservableCollection<string> RecentTicketNumbers { get; } = new();

    public string ScenarioName
    {
        get => _scenarioName;
        set
        {
            if (SetField(ref _scenarioName, value))
            {
                RaiseCanStartChanged();
            }
        }
    }

    public string? RootFolderPath
    {
        get => _rootFolderPath;
        set
        {
            if (SetField(ref _rootFolderPath, value))
            {
                RaiseCanStartChanged();
            }
        }
    }

    public bool IsSessionActive
    {
        get => _isSessionActive;
        private set
        {
            if (SetField(ref _isSessionActive, value))
            {
                RaiseCanStartChanged();
                ((RelayCommand)StopSessionCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public int ScreenshotCount
    {
        get => _screenshotCount;
        private set => SetField(ref _screenshotCount, value);
    }

    public string CaptureShortcut
    {
        get => _captureShortcut;
        private set => SetField(ref _captureShortcut, value);
    }

    public string? WarningText
    {
        get => _warningText;
        private set => SetField(ref _warningText, value);
    }

    public bool CopyToClipboard
    {
        get => _copyToClipboard;
        set
        {
            if (SetField(ref _copyToClipboard, value) && _isInitialized)
            {
                _ = SaveCopyToClipboardAsync(value);
            }
        }
    }

    public ICommand StartSessionCommand { get; }
    public ICommand StopSessionCommand { get; }
    public ICommand BrowseRootFolderCommand { get; }
    public ICommand ChangeShortcutCommand { get; }

    private bool CanStart =>
        !IsSessionActive
        && !string.IsNullOrWhiteSpace(TicketNumber)
        && !string.IsNullOrWhiteSpace(ScenarioName)
        && !string.IsNullOrWhiteSpace(RootFolderPath)
        && Directory.Exists(RootFolderPath);

    private void RaiseCanStartChanged() => ((RelayCommand)StartSessionCommand).RaiseCanExecuteChanged();

    public async Task InitializeAsync()
    {
        var config = await _configService.LoadAsync();
        RootFolderPath = config.RootFolderPath;
        CaptureShortcut = config.CaptureShortcut;
        CopyToClipboard = config.CopyToClipboard;
        SetRecentTicketNumbers(config.RecentTicketNumbers);
        _isInitialized = true;

        try
        {
            _hotkeyService.Register(CaptureShortcut);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(
                $"Could not register the capture shortcut \"{CaptureShortcut}\": {ex.Message}\n\nAnother application may already be using it. Use Change... next to the shortcut to pick a different one.",
                "Capture Shortcut Unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            WarningText = $"The capture shortcut \"{CaptureShortcut}\" is not registered. Use Change... to pick another.";
        }

        CaptureSession? resumed;
        try
        {
            resumed = await _sessionManager.TryResumeInterruptedSessionAsync();
        }
        catch (Exception ex)
        {
            // Never let a damaged previous session stop the app from starting.
            WarningText = $"Could not check for an interrupted session: {ex.Message}";
            return;
        }

        if (resumed is null)
        {
            return;
        }

        var choice = MessageBox.Show(
            $"An interrupted session was found for ticket \"{resumed.TicketNumber}\", scenario \"{resumed.ScenarioName}\" with {resumed.Screenshots.Count} screenshot(s). Resume it?",
            "Resume Interrupted Session",
            MessageBoxButton.YesNo);

        if (choice == MessageBoxResult.Yes)
        {
            _sessionManager.AcceptResume();
            TicketNumber = resumed.TicketNumber;
            ScenarioName = resumed.ScenarioName;
            ScreenshotCount = resumed.Screenshots.Count;
            IsSessionActive = true;
            StatusText = "Capturing (resumed)...";
        }
        else
        {
            await _sessionManager.DeclineResumeAsync();
        }
    }

    private async Task StartSessionAsync()
    {
        try
        {
            await _sessionManager.StartAsync(TicketNumber, ScenarioName);
            ScreenshotCount = 0;
            IsSessionActive = true;
            StatusText = "Capturing...";
            WarningText = null;

            var config = await _configService.LoadAsync();
            SetRecentTicketNumbers(config.RecentTicketNumbers);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Cannot Start Session", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task StopSessionAsync()
    {
        var session = _sessionManager.Current;
        await _sessionManager.StopAsync();
        IsSessionActive = false;
        StatusText = "Idle";

        if (session is { Screenshots.Count: > 0 })
        {
            SessionStoppedWithScreenshots?.Invoke(this, session);
        }
    }

    private async Task BrowseRootFolderAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select Root Evidence Folder" };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        RootFolderPath = dialog.FolderName;
        var config = await _configService.LoadAsync();
        config.RootFolderPath = RootFolderPath;
        await _configService.SaveAsync(config);
    }

    private async Task ChangeShortcutAsync()
    {
        var previous = CaptureShortcut;

        // Release the current shortcut while recording, so pressing it reaches the dialog
        // instead of taking a screenshot.
        _hotkeyService.Unregister();
        var dialog = new ShortcutWindow(previous) { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
        var requested = dialog.Shortcut ?? previous;

        try
        {
            _hotkeyService.Register(requested);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(
                $"\"{requested}\" could not be registered; another application may already be using it. Keeping \"{previous}\".",
                "Shortcut Unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            requested = previous;
            TryRegister(previous);
        }

        if (requested == previous)
        {
            return;
        }

        WarningText = null;
        CaptureShortcut = requested;
        var config = await _configService.LoadAsync();
        config.CaptureShortcut = requested;
        await _configService.SaveAsync(config);
    }

    private void TryRegister(string shortcut)
    {
        try
        {
            _hotkeyService.Register(shortcut);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            WarningText = $"The capture shortcut \"{shortcut}\" is not registered. Use Change... to pick another.";
        }
    }

    private async Task SaveCopyToClipboardAsync(bool value)
    {
        var config = await _configService.LoadAsync();
        config.CopyToClipboard = value;
        await _configService.SaveAsync(config);
    }

    private void SetRecentTicketNumbers(IEnumerable<string> ticketNumbers)
    {
        RecentTicketNumbers.Clear();
        foreach (var ticketNumber in ticketNumbers)
        {
            RecentTicketNumbers.Add(ticketNumber);
        }
    }

    private async Task OnHotkeyPressedAsync()
    {
        var screenshot = await _sessionManager.CaptureAsync();
        if (screenshot is null)
        {
            return;
        }

        ScreenshotCount = _sessionManager.Current?.Screenshots.Count ?? ScreenshotCount;
        _feedbackPresenter.PlayConfirmationSound();
        _feedbackPresenter.ShowFlash();

        if (CopyToClipboard && _sessionManager.Current is { } session
            && !ScreenshotClipboard.TryCopy(Path.Combine(session.FolderPath, screenshot.FinalFileName)))
        {
            WarningText = "Screenshot saved, but it could not be copied to the clipboard.";
        }
    }
}
