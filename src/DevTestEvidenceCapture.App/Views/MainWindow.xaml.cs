using System.Windows;
using DevTestEvidenceCapture.App.ViewModels;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ISessionManager _sessionManager;

    public MainWindow(MainViewModel viewModel, ISessionManager sessionManager)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _sessionManager = sessionManager;
        DataContext = _viewModel;

        _viewModel.SessionStoppedWithScreenshots += OnSessionStoppedWithScreenshots;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private void OnSessionStoppedWithScreenshots(object? sender, CaptureSession session)
    {
        var renameViewModel = new RenameViewModel(_sessionManager, session);
        var renameWindow = new RenameWindow(renameViewModel) { Owner = this };
        renameViewModel.RequestClose += (_, _) => renameWindow.Close();
        renameWindow.ShowDialog();
    }
}
