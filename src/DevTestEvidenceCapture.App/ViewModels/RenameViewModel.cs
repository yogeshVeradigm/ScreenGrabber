using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.App.ViewModels;

public sealed class RenameViewModel : ViewModelBase
{
    private readonly ISessionManager _sessionManager;
    private string? _conflictMessage;

    public RenameViewModel(ISessionManager sessionManager, CaptureSession session)
    {
        _sessionManager = sessionManager;
        Rows = new ObservableCollection<ScreenshotRenameRow>(
            session.Screenshots
                .OrderBy(s => s.SequenceNumber)
                .Select(s => new ScreenshotRenameRow(s.SequenceNumber, s.FinalFileName, session.FolderPath, s.Note)));

        ConfirmCommand = new RelayCommand(ConfirmAsync);
        SkipCommand = new RelayCommand(SkipAsync);
    }

    public event EventHandler? RequestClose;

    public ObservableCollection<ScreenshotRenameRow> Rows { get; }

    public string? ConflictMessage
    {
        get => _conflictMessage;
        private set => SetField(ref _conflictMessage, value);
    }

    public ICommand ConfirmCommand { get; }
    public ICommand SkipCommand { get; }

    private async Task ConfirmAsync()
    {
        foreach (var row in Rows)
        {
            row.HasConflict = false;
        }

        var map = Rows.ToDictionary(r => r.SequenceNumber, r => r.MeaningfulName);
        var result = await _sessionManager.RenameAsync(map);

        if (result.Succeeded)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (result.ConflictingSequenceNumbers.Count > 0)
        {
            foreach (var row in Rows.Where(r => result.ConflictingSequenceNumbers.Contains(r.SequenceNumber)))
            {
                row.HasConflict = true;
            }

            ConflictMessage = "Two or more screenshots have the same name. Fix the highlighted rows before confirming.";
            return;
        }

        MessageBox.Show(
            $"Renaming stopped partway through: {result.Error?.Message}. Screenshots already renamed keep their new names; the rest keep their original names.",
            "Renaming Incomplete",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private async Task SkipAsync()
    {
        await _sessionManager.SkipRenameAsync();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
