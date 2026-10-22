using System.IO;
using System.Windows.Media.Imaging;

namespace DevTestEvidenceCapture.App.ViewModels;

public sealed class ScreenshotRenameRow : ViewModelBase
{
    private const int ThumbnailDecodePixelWidth = 64;

    private string? _meaningfulName;
    private bool _hasConflict;

    public ScreenshotRenameRow(int sequenceNumber, string currentFileName, string folderPath, string? note = null)
    {
        SequenceNumber = sequenceNumber;
        CurrentFileName = currentFileName;
        Note = note;
        FullFilePath = Path.Combine(folderPath, currentFileName);
        Thumbnail = LoadThumbnail(FullFilePath);
    }

    public int SequenceNumber { get; }

    public string CurrentFileName { get; }

    /// <summary>The note typed at capture time, shown as a reminder while naming.</summary>
    public string? Note { get; }

    public string FullFilePath { get; }

    public BitmapImage? Thumbnail { get; }

    public string? MeaningfulName
    {
        get => _meaningfulName;
        set => SetField(ref _meaningfulName, value);
    }

    public bool HasConflict
    {
        get => _hasConflict;
        set => SetField(ref _hasConflict, value);
    }

    private static BitmapImage? LoadThumbnail(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = ThumbnailDecodePixelWidth;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
