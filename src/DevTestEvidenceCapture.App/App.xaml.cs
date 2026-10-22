using System.Windows;
using System.Windows.Interop;
using DevTestEvidenceCapture.App.Capture;
using DevTestEvidenceCapture.App.Feedback;
using DevTestEvidenceCapture.App.ViewModels;
using DevTestEvidenceCapture.App.Views;
using DevTestEvidenceCapture.Core.Capture;
using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Hotkey;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;

namespace DevTestEvidenceCapture.App;

public partial class App : Application
{
    private IGlobalHotkeyService? _hotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var namingService = new NamingService();
        var metadataService = new ScenarioMetadataService();
        var storage = new EvidenceStorage(namingService, metadataService);
        var captureService = new ScreenCaptureService();
        var configService = new AppConfigurationService();
        var sessionManager = new SessionManager(storage, captureService, metadataService, namingService, configService, new OverlayRegionSelector(configService));
        _hotkeyService = new GlobalHotkeyService();

        var mainViewModel = new MainViewModel(sessionManager, _hotkeyService, configService, new CaptureFeedbackPresenter());
        var mainWindow = new MainWindow(mainViewModel, sessionManager);

        mainWindow.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(mainWindow).Handle;
            HwndSource.FromHwnd(handle)!.AddHook(WndProc);

            // The shortcut itself is registered by MainViewModel once config.json has been loaded.
            _hotkeyService.AttachWindow(handle);
        };
        mainWindow.Closed += (_, _) => _hotkeyService?.Unregister();

        mainWindow.Show();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeyService!.ProcessWindowMessage(msg, wParam))
        {
            handled = true;
        }

        return IntPtr.Zero;
    }
}
