using System.Drawing;

namespace DevTestEvidenceCapture.Core.Capture;

/// <summary>
/// A mark drawn on a capture before it is saved. Coordinates and sizes are in physical pixels of the
/// full captured image (not relative to the selected region).
/// </summary>
public abstract record Annotation;

/// <summary>A freehand, semi-transparent highlighter stroke.</summary>
public sealed record HighlighterStroke(IReadOnlyList<PointF> Points, float Width, int Argb) : Annotation;

public enum ShapeKind
{
    Rectangle,
    Ellipse,
}

/// <summary>
/// A rectangle or ellipse. When not filled, the border is drawn inside <see cref="Bounds"/>.
/// </summary>
public sealed record ShapeAnnotation(ShapeKind Kind, RectangleF Bounds, float StrokeWidth, bool Filled, int Argb) : Annotation;

/// <summary>
/// Replaces an area with coarse blocks of its average colour, e.g. to hide patient details. The
/// blocks are computed from the original capture, so it covers any annotation drawn earlier.
/// </summary>
public sealed record PixelateAnnotation(Rectangle Bounds, int BlockSize) : Annotation;
