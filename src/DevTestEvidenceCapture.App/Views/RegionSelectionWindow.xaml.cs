using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevTestEvidenceCapture.Core.Capture;
using DevTestEvidenceCapture.Core.Configuration;
using DrawingPointF = System.Drawing.PointF;
using DrawingRectangleF = System.Drawing.RectangleF;

namespace DevTestEvidenceCapture.App.Views;

/// <summary>
/// Full-screen overlay showing a frozen image of the primary monitor, on which the user drags a
/// rectangle to choose what to save (similar to the Windows Snipping Tool), then optionally marks it
/// up with a highlighter, rectangle/ellipse or pixelation and adds a note before saving.
/// </summary>
public partial class RegionSelectionWindow : Window
{
    // Drags smaller than these (in DIPs) are treated as accidental clicks and ignored.
    private const double MinimumSelectionSize = 4;
    private const double MinimumShapeSize = 2;

    private const double ToolbarGap = 8;

    private static readonly Color HighlighterColor = Color.FromArgb(110, 255, 255, 0);
    private static readonly (double Min, double Max) HighlighterSizeRange = (4, 60);
    private static readonly (double Min, double Max) ShapeStrokeWidthRange = (1, 16);
    private static readonly (double Min, double Max) PixelateBlockSizeRange = (4, 30);

    private enum Tool
    {
        Select,
        Highlighter,
        Rectangle,
        Ellipse,
        Pixelate,
    }

    private readonly BitmapImage _screenImage;
    private readonly AnnotationSettings _settings;
    private readonly RectangleGeometry _fullArea = new();
    private readonly RectangleGeometry _selectionArea = new();
    private readonly List<(UIElement Visual, Annotation Annotation)> _annotations = new();

    // Suppresses toolbar change handlers while the toolbar is being set up from code.
    private bool _isUpdatingToolbar = true;
    private Tool _tool = Tool.Select;
    private Rect? _selection;
    private Point? _dragStart;
    private Polyline? _activeStroke;
    private Shape? _activeShape;

    public RegionSelectionWindow(byte[] screenPng, AnnotationSettings settings)
    {
        _settings = settings;
        _settings.HighlighterSize = Math.Clamp(_settings.HighlighterSize, HighlighterSizeRange.Min, HighlighterSizeRange.Max);
        _settings.ShapeStrokeWidth = Math.Clamp(_settings.ShapeStrokeWidth, ShapeStrokeWidthRange.Min, ShapeStrokeWidthRange.Max);
        _settings.PixelateBlockSize = Math.Clamp(_settings.PixelateBlockSize, PixelateBlockSizeRange.Min, PixelateBlockSizeRange.Max);

        InitializeComponent();

        _screenImage = LoadImage(screenPng);
        FrozenScreen.Source = _screenImage;

        // Primary monitor bounds, matching what ScreenCaptureService captured.
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        DimOverlay.Data = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd,
            Children = { _fullArea, _selectionArea },
        };

        FilledCheckBox.IsChecked = _settings.ShapeFilled;
        var colorButton = ColorPalette.Children.OfType<RadioButton>()
            .FirstOrDefault(b => string.Equals((string)b.Tag, _settings.ShapeColor, StringComparison.OrdinalIgnoreCase))
            ?? RedColorButton;
        colorButton.IsChecked = true;
        _settings.ShapeColor = (string)colorButton.Tag;
        SelectToolButton.IsChecked = true;
        _isUpdatingToolbar = false;

        Surface.SizeChanged += (_, _) => _fullArea.Rect = new Rect(0, 0, Surface.ActualWidth, Surface.ActualHeight);
        ContentRendered += (_, _) =>
        {
            Activate();
            Keyboard.Focus(this);
        };
    }

    /// <summary>What to save, or <c>null</c> if the capture was cancelled.</summary>
    public CaptureSelection? Result { get; private set; }

    /// <summary>The annotation settings, including any changes the user made in this overlay.</summary>
    public AnnotationSettings Settings => _settings;

    private double ScaleX => _screenImage.PixelWidth / Surface.ActualWidth;

    private double ScaleY => _screenImage.PixelHeight / Surface.ActualHeight;

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.Enter)
        {
            Save();
        }
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && !NoteTextBox.IsKeyboardFocused)
        {
            Undo();
        }
        else
        {
            return;
        }

        e.Handled = true;
    }

    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Return keyboard focus from the note box so Ctrl+Z undoes drawing again.
        if (NoteTextBox.IsKeyboardFocused)
        {
            Focus();
        }

        var point = ClampToSurface(e.GetPosition(Surface));

        if (_selection is { } selection && _tool != Tool.Select)
        {
            if (!selection.Contains(point))
            {
                return;
            }

            BeginAnnotation(point);
        }
        else
        {
            HintBanner.Visibility = Visibility.Collapsed;
            Toolbar.Visibility = Visibility.Collapsed;
            ShowSelection(new Rect(point, point));
        }

        _dragStart = point;
        Surface.CaptureMouse();
    }

    private void Surface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not { } start)
        {
            return;
        }

        var point = ClampToSurface(e.GetPosition(Surface));
        if (_activeStroke is not null)
        {
            _activeStroke.Points.Add(ClampToSelection(point));
        }
        else if (_activeShape is not null)
        {
            LayoutShape(_activeShape, ShapeBounds(start, point));
        }
        else
        {
            ShowSelection(new Rect(start, point));
        }
    }

    private void Surface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart is not { } start)
        {
            return;
        }

        _dragStart = null;
        Surface.ReleaseMouseCapture();

        var point = ClampToSurface(e.GetPosition(Surface));
        if (_activeStroke is not null)
        {
            CommitStroke();
        }
        else if (_activeShape is not null && _tool == Tool.Pixelate)
        {
            CommitPixelate(ShapeBounds(start, point));
        }
        else if (_activeShape is not null)
        {
            CommitShape(ShapeBounds(start, point));
        }
        else
        {
            CommitSelection(new Rect(start, point));
        }
    }

    // Right-click cancels only before an area is chosen, so it can't throw away annotations by accident.
    private void Surface_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_selection is null && _dragStart is null)
        {
            Close();
        }
    }

    private void ToolButton_Checked(object sender, RoutedEventArgs e)
    {
        _tool = sender == HighlighterToolButton ? Tool.Highlighter
            : sender == RectangleToolButton ? Tool.Rectangle
            : sender == EllipseToolButton ? Tool.Ellipse
            : sender == PixelateToolButton ? Tool.Pixelate
            : Tool.Select;

        var wasUpdatingToolbar = _isUpdatingToolbar;
        _isUpdatingToolbar = true;
        if (_tool == Tool.Highlighter)
        {
            SetSlider(HighlighterSizeRange, _settings.HighlighterSize);
        }
        else if (_tool is Tool.Rectangle or Tool.Ellipse)
        {
            SetSlider(ShapeStrokeWidthRange, _settings.ShapeStrokeWidth);
        }
        else if (_tool == Tool.Pixelate)
        {
            SetSlider(PixelateBlockSizeRange, _settings.PixelateBlockSize);
        }

        _isUpdatingToolbar = wasUpdatingToolbar;

        SizeOptions.IsEnabled = _tool != Tool.Select;
        ShapeOptions.IsEnabled = _tool is Tool.Rectangle or Tool.Ellipse;
        SizeValueText.Text = _tool == Tool.Select ? string.Empty : $"{SizeSlider.Value:0}";
    }

    private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingToolbar)
        {
            return;
        }

        if (_tool == Tool.Highlighter)
        {
            _settings.HighlighterSize = e.NewValue;
        }
        else if (_tool is Tool.Rectangle or Tool.Ellipse)
        {
            _settings.ShapeStrokeWidth = e.NewValue;
        }
        else if (_tool == Tool.Pixelate)
        {
            _settings.PixelateBlockSize = e.NewValue;
        }

        SizeValueText.Text = $"{e.NewValue:0}";
    }

    private void FilledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isUpdatingToolbar)
        {
            _settings.ShapeFilled = FilledCheckBox.IsChecked == true;
        }
    }

    private void ColorButton_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isUpdatingToolbar)
        {
            _settings.ShapeColor = (string)((FrameworkElement)sender).Tag;
        }
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e) => Undo();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void SaveButton_Click(object sender, RoutedEventArgs e) => Save();

    private void Save()
    {
        if (_dragStart is not null)
        {
            return;
        }

        // With no area chosen yet, Enter saves the whole screen.
        var region = _selection is { } selection
            ? ToImagePixels(selection)
            : new CaptureRegion(0, 0, _screenImage.PixelWidth, _screenImage.PixelHeight);
        Result = new CaptureSelection(region, _annotations.Select(a => a.Annotation).ToList(), NoteTextBox.Text);
        Close();
    }

    private void Undo()
    {
        if (_dragStart is not null || _annotations.Count == 0)
        {
            return;
        }

        var last = _annotations[^1];
        _annotations.RemoveAt(_annotations.Count - 1);
        AnnotationLayer.Children.Remove(last.Visual);
    }

    private void CommitSelection(Rect rect)
    {
        if (rect.Width < MinimumSelectionSize || rect.Height < MinimumSelectionSize)
        {
            // Treat as a stray click: keep whatever was selected before.
            ShowSelection(_selection);
            if (_selection is null)
            {
                HintBanner.Visibility = Visibility.Visible;
            }
            else
            {
                ShowToolbar();
            }

            return;
        }

        _selection = rect;
        HintBanner.Visibility = Visibility.Collapsed;
        ShowSelection(rect);
        ShowToolbar();
    }

    private void BeginAnnotation(Point point)
    {
        if (_tool == Tool.Highlighter)
        {
            _activeStroke = new Polyline
            {
                Stroke = new SolidColorBrush(HighlighterColor),
                StrokeThickness = _settings.HighlighterSize,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Points = { point, point },
            };
            AnnotationLayer.Children.Add(_activeStroke);
            return;
        }

        if (_tool == Tool.Pixelate)
        {
            // Dashed marker while dragging; replaced by the pixelated preview on release.
            _activeShape = new Rectangle
            {
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                StrokeDashArray = { 4, 3 },
                Fill = new SolidColorBrush(Color.FromArgb(70, 128, 128, 128)),
            };
            LayoutShape(_activeShape, new Rect(point, point));
            AnnotationLayer.Children.Add(_activeShape);
            return;
        }

        var brush = new SolidColorBrush(ParseColor(_settings.ShapeColor));
        _activeShape = _tool == Tool.Rectangle ? new Rectangle() : new Ellipse();
        _activeShape.Stroke = brush;
        _activeShape.StrokeThickness = _settings.ShapeStrokeWidth;
        _activeShape.Fill = _settings.ShapeFilled ? brush : null;
        LayoutShape(_activeShape, new Rect(point, point));
        AnnotationLayer.Children.Add(_activeShape);
    }

    private void CommitStroke()
    {
        var stroke = _activeStroke!;
        _activeStroke = null;

        var annotation = new HighlighterStroke(
            stroke.Points.Select(ToImagePoint).ToList(),
            ToImageLength(stroke.StrokeThickness),
            ToArgb(HighlighterColor));
        _annotations.Add((stroke, annotation));
    }

    private void CommitShape(Rect bounds)
    {
        var shape = _activeShape!;
        _activeShape = null;

        if (bounds.Width < MinimumShapeSize || bounds.Height < MinimumShapeSize)
        {
            AnnotationLayer.Children.Remove(shape);
            return;
        }

        LayoutShape(shape, bounds);
        var annotation = new ShapeAnnotation(
            shape is Ellipse ? ShapeKind.Ellipse : ShapeKind.Rectangle,
            ToImageRect(bounds),
            ToImageLength(shape.StrokeThickness),
            shape.Fill is not null,
            ToArgb(((SolidColorBrush)shape.Stroke).Color));
        _annotations.Add((shape, annotation));
    }

    private void CommitPixelate(Rect bounds)
    {
        AnnotationLayer.Children.Remove(_activeShape!);
        _activeShape = null;

        if (bounds.Width < MinimumShapeSize || bounds.Height < MinimumShapeSize)
        {
            return;
        }

        var region = ToImagePixels(bounds);
        var left = Math.Max(0, region.X);
        var top = Math.Max(0, region.Y);
        var width = Math.Min(_screenImage.PixelWidth, region.X + region.Width) - left;
        var height = Math.Min(_screenImage.PixelHeight, region.Y + region.Height) - top;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // Same block maths as CaptureImageRenderer, so the preview matches the saved file.
        var blockSize = Math.Max(2, (int)Math.Round(ToImageLength(_settings.PixelateBlockSize)));
        var blocksWide = (int)Math.Ceiling(width / (double)blockSize);
        var blocksHigh = (int)Math.Ceiling(height / (double)blockSize);
        var blocks = new TransformedBitmap(
            new CroppedBitmap(_screenImage, new Int32Rect(left, top, width, height)),
            new ScaleTransform(blocksWide / (double)width, blocksHigh / (double)height));
        blocks.Freeze();

        var preview = new Image
        {
            Source = blocks,
            Stretch = Stretch.Fill,
            Width = width / ScaleX,
            Height = height / ScaleY,
        };
        RenderOptions.SetBitmapScalingMode(preview, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(preview, left / ScaleX);
        Canvas.SetTop(preview, top / ScaleY);
        AnnotationLayer.Children.Add(preview);

        var annotation = new PixelateAnnotation(new System.Drawing.Rectangle(left, top, width, height), blockSize);
        _annotations.Add((preview, annotation));
    }

    // Holding Shift constrains the shape to a square/circle that still fits inside the selection.
    private Rect ShapeBounds(Point start, Point current)
    {
        var selection = _selection!.Value;
        var end = ClampToSelection(current);

        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var maxX = dx >= 0 ? selection.Right - start.X : start.X - selection.Left;
            var maxY = dy >= 0 ? selection.Bottom - start.Y : start.Y - selection.Top;
            var side = Math.Min(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Min(maxX, maxY));
            end = new Point(start.X + (dx < 0 ? -side : side), start.Y + (dy < 0 ? -side : side));
        }

        return new Rect(start, end);
    }

    private static void LayoutShape(Shape shape, Rect bounds)
    {
        shape.Width = bounds.Width;
        shape.Height = bounds.Height;
        Canvas.SetLeft(shape, bounds.X);
        Canvas.SetTop(shape, bounds.Y);
    }

    private void ShowSelection(Rect? rect)
    {
        if (rect is not { } selection)
        {
            _selectionArea.Rect = Rect.Empty;
            SelectionBorder.Visibility = Visibility.Collapsed;
            SizeLabel.Visibility = Visibility.Collapsed;
            return;
        }

        _selectionArea.Rect = selection;
        AnnotationLayer.Clip = new RectangleGeometry(selection);

        SelectionBorder.Visibility = Visibility.Visible;
        SelectionBorder.Width = selection.Width;
        SelectionBorder.Height = selection.Height;
        Canvas.SetLeft(SelectionBorder, selection.X);
        Canvas.SetTop(SelectionBorder, selection.Y);

        var pixels = ToImagePixels(selection);
        SizeText.Text = $"{pixels.Width} x {pixels.Height}";
        SizeLabel.Visibility = Visibility.Visible;
        Canvas.SetLeft(SizeLabel, selection.X);
        Canvas.SetTop(SizeLabel, selection.Y >= 26 ? selection.Y - 26 : selection.Bottom + 4);
    }

    // Places the toolbar below the selection, else above it, else just inside its bottom edge.
    private void ShowToolbar()
    {
        var selection = _selection!.Value;
        Toolbar.Visibility = Visibility.Visible;
        Toolbar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = Toolbar.DesiredSize;

        var top = selection.Bottom + ToolbarGap;
        if (top + size.Height > Surface.ActualHeight)
        {
            top = selection.Top - ToolbarGap - size.Height;
        }

        if (top < 0)
        {
            top = selection.Bottom - ToolbarGap - size.Height;
        }

        Canvas.SetTop(Toolbar, Math.Clamp(top, 0, Math.Max(0, Surface.ActualHeight - size.Height)));
        Canvas.SetLeft(Toolbar, Math.Clamp(selection.Left, 0, Math.Max(0, Surface.ActualWidth - size.Width)));
    }

    private void SetSlider((double Min, double Max) range, double value)
    {
        SizeSlider.Minimum = range.Min;
        SizeSlider.Maximum = range.Max;
        SizeSlider.Value = value;
    }

    private Point ClampToSurface(Point point) => new(
        Math.Clamp(point.X, 0, Surface.ActualWidth),
        Math.Clamp(point.Y, 0, Surface.ActualHeight));

    private Point ClampToSelection(Point point)
    {
        var selection = _selection!.Value;
        return new Point(
            Math.Clamp(point.X, selection.Left, selection.Right),
            Math.Clamp(point.Y, selection.Top, selection.Bottom));
    }

    // The frozen image is stretched over the whole window, so DIPs map to image pixels proportionally.
    // This keeps the crop and annotations accurate regardless of the display scaling factor.
    private CaptureRegion ToImagePixels(Rect selection)
    {
        var left = (int)Math.Floor(selection.Left * ScaleX);
        var top = (int)Math.Floor(selection.Top * ScaleY);
        var right = (int)Math.Ceiling(selection.Right * ScaleX);
        var bottom = (int)Math.Ceiling(selection.Bottom * ScaleY);
        return new CaptureRegion(left, top, right - left, bottom - top);
    }

    private DrawingPointF ToImagePoint(Point point) => new((float)(point.X * ScaleX), (float)(point.Y * ScaleY));

    private DrawingRectangleF ToImageRect(Rect rect) => new(
        (float)(rect.X * ScaleX),
        (float)(rect.Y * ScaleY),
        (float)(rect.Width * ScaleX),
        (float)(rect.Height * ScaleY));

    private float ToImageLength(double length) => (float)(length * (ScaleX + ScaleY) / 2);

    private static int ToArgb(Color color) => (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

    private static Color ParseColor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch (FormatException)
        {
            return Colors.Red;
        }
    }

    private static BitmapImage LoadImage(byte[] png)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = new MemoryStream(png);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
