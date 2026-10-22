using System.Drawing;
using System.Drawing.Imaging;

namespace DevTestEvidenceCapture.Core.Tests.Capture;

internal static class TestPngFactory
{
    /// <summary>Creates a PNG whose left half is red and right half is blue.</summary>
    public static byte[] CreateSplitPng(int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.FillRectangle(Brushes.Red, 0, 0, width / 2, height);
            graphics.FillRectangle(Brushes.Blue, width / 2, 0, width - width / 2, height);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    public static Bitmap Decode(byte[] png) => new(new MemoryStream(png));
}
