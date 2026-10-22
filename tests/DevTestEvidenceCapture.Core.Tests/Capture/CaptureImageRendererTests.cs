using System.Drawing;
using DevTestEvidenceCapture.Core.Capture;

namespace DevTestEvidenceCapture.Core.Tests.Capture;

public class CaptureImageRendererTests
{
    private static readonly int Green = Color.FromArgb(255, 0, 160, 0).ToArgb();
    private static readonly int TranslucentYellow = Color.FromArgb(110, 255, 255, 0).ToArgb();

    [Fact]
    public void Render_ReturnsOnlyTheSelectedRegion()
    {
        var png = TestPngFactory.CreateSplitPng(20, 10);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(12, 2, 5, 4));

        using var bitmap = TestPngFactory.Decode(rendered);
        Assert.Equal(5, bitmap.Width);
        Assert.Equal(4, bitmap.Height);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(4, 3).ToArgb());
    }

    [Fact]
    public void Render_WithRegionExtendingPastTheImage_ClampsToImageBounds()
    {
        var png = TestPngFactory.CreateSplitPng(20, 10);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(15, 5, 50, 50));

        using var bitmap = TestPngFactory.Decode(rendered);
        Assert.Equal(5, bitmap.Width);
        Assert.Equal(5, bitmap.Height);
    }

    [Fact]
    public void Render_WithRegionOutsideTheImage_Throws()
    {
        var png = TestPngFactory.CreateSplitPng(20, 10);

        Assert.Throws<ArgumentException>(() => CaptureImageRenderer.Render(png, new CaptureRegion(30, 30, 5, 5)));
    }

    [Fact]
    public void Render_FilledRectangle_FillsItsWholeArea()
    {
        var png = TestPngFactory.CreateSplitPng(40, 40);
        var shape = new ShapeAnnotation(ShapeKind.Rectangle, new RectangleF(5, 5, 20, 20), 3, Filled: true, Green);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(0, 0, 40, 40), new[] { shape });

        using var bitmap = TestPngFactory.Decode(rendered);
        Assert.Equal(Green, bitmap.GetPixel(15, 15).ToArgb());
        Assert.Equal(Green, bitmap.GetPixel(6, 6).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
    }

    [Fact]
    public void Render_OutlinedRectangle_DrawsBorderInsideBoundsAndLeavesCentreUntouched()
    {
        var png = TestPngFactory.CreateSplitPng(40, 40);
        var shape = new ShapeAnnotation(ShapeKind.Rectangle, new RectangleF(5, 5, 20, 20), 4, Filled: false, Green);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(0, 0, 40, 40), new[] { shape });

        using var bitmap = TestPngFactory.Decode(rendered);
        Assert.Equal(Green, bitmap.GetPixel(6, 15).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(15, 15).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(3, 15).ToArgb());
    }

    [Fact]
    public void Render_FilledEllipse_FillsCentreButNotCorners()
    {
        var png = TestPngFactory.CreateSplitPng(40, 40);
        var shape = new ShapeAnnotation(ShapeKind.Ellipse, new RectangleF(0, 0, 20, 20), 3, Filled: true, Green);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(0, 0, 40, 40), new[] { shape });

        using var bitmap = TestPngFactory.Decode(rendered);
        Assert.Equal(Green, bitmap.GetPixel(10, 10).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
    }

    [Fact]
    public void Render_HighlighterStroke_TintsUnderlyingPixelsWithoutHidingThem()
    {
        var png = TestPngFactory.CreateSplitPng(40, 40);
        var stroke = new HighlighterStroke(new[] { new PointF(30, 20), new PointF(38, 20) }, 6, TranslucentYellow);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(0, 0, 40, 40), new[] { stroke });

        using var bitmap = TestPngFactory.Decode(rendered);
        var tinted = bitmap.GetPixel(34, 20);
        Assert.True(tinted.R > 0 && tinted.G > 0, "Yellow should be blended in.");
        Assert.True(tinted.B > 0, "The blue underneath should still show through.");
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(34, 30).ToArgb());
    }

    [Fact]
    public void Render_Pixelate_TurnsAreaIntoFlatOpaqueBlocksAndLeavesTheRestUntouched()
    {
        var png = CreateStripedPng(40, 40);
        var pixelate = new PixelateAnnotation(new Rectangle(0, 0, 20, 20), BlockSize: 10);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(0, 0, 40, 40), new[] { pixelate });

        using var bitmap = TestPngFactory.Decode(rendered);
        for (var blockX = 0; blockX < 20; blockX += 10)
        {
            for (var blockY = 0; blockY < 20; blockY += 10)
            {
                var first = bitmap.GetPixel(blockX, blockY);
                Assert.Equal(255, first.A);
                for (var x = blockX; x < blockX + 10; x++)
                {
                    for (var y = blockY; y < blockY + 10; y++)
                    {
                        Assert.Equal(first.ToArgb(), bitmap.GetPixel(x, y).ToArgb());
                    }
                }
            }
        }

        // Outside the pixelated area the 1px black/white stripes are intact.
        Assert.Equal(Color.Black.ToArgb(), bitmap.GetPixel(30, 30).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(31, 30).ToArgb());
    }

    [Fact]
    public void Render_Pixelate_HidesFineDetail()
    {
        var png = CreateStripedPng(40, 40);
        var pixelate = new PixelateAnnotation(new Rectangle(0, 0, 40, 40), BlockSize: 8);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(0, 0, 40, 40), new[] { pixelate });

        // Alternating black/white stripes average to mid grey, i.e. the stripes are gone.
        using var bitmap = TestPngFactory.Decode(rendered);
        var block = bitmap.GetPixel(12, 12);
        Assert.InRange(block.R, 60, 195);
        Assert.Equal(block.ToArgb(), bitmap.GetPixel(13, 12).ToArgb());
    }

    private static byte[] CreateStripedPng(int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, x % 2 == 0 ? Color.Black : Color.White);
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    [Fact]
    public void Render_AnnotationsUseFullImageCoordinates_AndAreOffsetByTheRegion()
    {
        var png = TestPngFactory.CreateSplitPng(40, 40);
        var shape = new ShapeAnnotation(ShapeKind.Rectangle, new RectangleF(20, 20, 10, 10), 2, Filled: true, Green);

        var rendered = CaptureImageRenderer.Render(png, new CaptureRegion(15, 15, 20, 20), new[] { shape });

        using var bitmap = TestPngFactory.Decode(rendered);
        Assert.Equal(Green, bitmap.GetPixel(10, 10).ToArgb());
        Assert.NotEqual(Green, bitmap.GetPixel(2, 2).ToArgb());
    }
}
