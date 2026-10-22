namespace DevTestEvidenceCapture.Core.Configuration;

/// <summary>Last-used annotation tool settings, remembered between captures and app launches.</summary>
public sealed class AnnotationSettings
{
    /// <summary>Highlighter brush size, in device-independent pixels.</summary>
    public double HighlighterSize { get; set; } = 20;

    /// <summary>Rectangle/ellipse border thickness, in device-independent pixels.</summary>
    public double ShapeStrokeWidth { get; set; } = 3;

    public bool ShapeFilled { get; set; }

    /// <summary>Rectangle/ellipse colour as <c>#RRGGBB</c>.</summary>
    public string ShapeColor { get; set; } = "#FF0000";

    /// <summary>Pixelate block size, in device-independent pixels.</summary>
    public double PixelateBlockSize { get; set; } = 10;
}
