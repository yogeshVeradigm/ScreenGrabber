using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DevTestEvidenceCapture.Core.Capture;

/// <summary>Crops a captured PNG to the selected region and draws the user's annotations onto it.</summary>
public static class CaptureImageRenderer
{
    public static byte[] Render(byte[] png, CaptureRegion region, IReadOnlyList<Annotation>? annotations = null)
    {
        using var input = new MemoryStream(png);
        using var source = new Bitmap(input);

        var bounds = Rectangle.Intersect(
            new Rectangle(0, 0, source.Width, source.Height),
            new Rectangle(region.X, region.Y, region.Width, region.Height));
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentException("The selected region does not overlap the captured image.", nameof(region));
        }

        using var output = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(output))
        {
            // Exact 1:1 pixel copy of the selected region.
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(source, new Rectangle(0, 0, bounds.Width, bounds.Height), bounds, GraphicsUnit.Pixel);

            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.PixelOffsetMode = PixelOffsetMode.Default;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TranslateTransform(-bounds.X, -bounds.Y);

            foreach (var annotation in annotations ?? Array.Empty<Annotation>())
            {
                Draw(graphics, annotation, source);
            }
        }

        using var outputStream = new MemoryStream();
        output.Save(outputStream, ImageFormat.Png);
        return outputStream.ToArray();
    }

    private static void Draw(Graphics graphics, Annotation annotation, Bitmap source)
    {
        switch (annotation)
        {
            case HighlighterStroke stroke:
                DrawHighlighterStroke(graphics, stroke);
                break;
            case ShapeAnnotation shape:
                DrawShape(graphics, shape);
                break;
            case PixelateAnnotation pixelate:
                DrawPixelation(graphics, pixelate, source);
                break;
        }
    }

    private static void DrawPixelation(Graphics graphics, PixelateAnnotation pixelate, Bitmap source)
    {
        var bounds = Rectangle.Intersect(pixelate.Bounds, new Rectangle(0, 0, source.Width, source.Height));
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var blockSize = Math.Max(2, pixelate.BlockSize);
        var blocksWide = (int)Math.Ceiling(bounds.Width / (double)blockSize);
        var blocksHigh = (int)Math.Ceiling(bounds.Height / (double)blockSize);

        // Shrink the area to one pixel per block (averaging its colours), then scale it back up
        // without smoothing so each block is a flat colour.
        using var blocks = new Bitmap(blocksWide, blocksHigh, PixelFormat.Format32bppArgb);
        using (var blockGraphics = Graphics.FromImage(blocks))
        using (var edgeMode = new ImageAttributes())
        {
            // Mirror edge pixels instead of blending in transparency at the image border.
            edgeMode.SetWrapMode(WrapMode.TileFlipXY);
            blockGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            blockGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            blockGraphics.DrawImage(
                source,
                new Rectangle(0, 0, blocksWide, blocksHigh),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                GraphicsUnit.Pixel,
                edgeMode);
        }

        var state = graphics.Save();
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.DrawImage(blocks, bounds, new Rectangle(0, 0, blocksWide, blocksHigh), GraphicsUnit.Pixel);
        graphics.Restore(state);
    }

    private static void DrawHighlighterStroke(Graphics graphics, HighlighterStroke stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        var color = Color.FromArgb(stroke.Argb);
        var first = stroke.Points[0];
        if (stroke.Points.All(p => p == first))
        {
            // A click without movement leaves a round dot, matching the on-screen preview.
            using var brush = new SolidBrush(color);
            graphics.FillEllipse(brush, first.X - stroke.Width / 2, first.Y - stroke.Width / 2, stroke.Width, stroke.Width);
            return;
        }

        // Drawn as a single polyline so the stroke's own overlaps don't darken.
        using var pen = new Pen(color, stroke.Width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        graphics.DrawLines(pen, stroke.Points.ToArray());
    }

    private static void DrawShape(Graphics graphics, ShapeAnnotation shape)
    {
        var color = Color.FromArgb(shape.Argb);
        var bounds = shape.Bounds;
        var border = new RectangleF(
            bounds.X + shape.StrokeWidth / 2,
            bounds.Y + shape.StrokeWidth / 2,
            bounds.Width - shape.StrokeWidth,
            bounds.Height - shape.StrokeWidth);

        if (shape.Filled || border.Width <= 0 || border.Height <= 0)
        {
            using var brush = new SolidBrush(color);
            if (shape.Kind == ShapeKind.Ellipse)
            {
                graphics.FillEllipse(brush, bounds);
            }
            else
            {
                graphics.FillRectangle(brush, bounds);
            }

            return;
        }

        using var pen = new Pen(color, shape.StrokeWidth);
        if (shape.Kind == ShapeKind.Ellipse)
        {
            graphics.DrawEllipse(pen, border);
        }
        else
        {
            graphics.DrawRectangle(pen, border.X, border.Y, border.Width, border.Height);
        }
    }
}
