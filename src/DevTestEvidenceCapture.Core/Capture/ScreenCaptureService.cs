using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DevTestEvidenceCapture.Core.Capture;

public sealed class ScreenCaptureService : IScreenCaptureService
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    public Task<byte[]> CapturePrimaryMonitorPngAsync()
    {
        var width = GetSystemMetrics(SM_CXSCREEN);
        var height = GetSystemMetrics(SM_CYSCREEN);

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height));

        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        return Task.FromResult(memoryStream.ToArray());
    }
}
