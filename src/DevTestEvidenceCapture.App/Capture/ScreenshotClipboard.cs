using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DevTestEvidenceCapture.App.Capture;

public static class ScreenshotClipboard
{
    /// <summary>
    /// Puts the saved screenshot on the clipboard as both a bitmap and PNG (which apps such as
    /// Teams and browsers prefer). Returns false if the clipboard is busy or the file can't be read.
    /// </summary>
    public static bool TryCopy(string pngPath)
    {
        try
        {
            var bytes = File.ReadAllBytes(pngPath);
            var image = BitmapFrame.Create(new MemoryStream(bytes), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            var data = new DataObject();
            data.SetImage(image);
            data.SetData("PNG", new MemoryStream(bytes));
            Clipboard.SetDataObject(data, copy: true);
            return true;
        }
        catch (Exception ex) when (ex is ExternalException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
