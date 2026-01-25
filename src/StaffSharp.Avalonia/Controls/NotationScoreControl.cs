using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using StaffSharp.Notation;

namespace StaffSharp.Avalonia.Controls;

/// <summary>
/// Avalonia control for rendering musical notation scores with automatic layout and highlighting support.
/// </summary>
public sealed class NotationScoreControl : Control, IDisposable
{
    private readonly SvgContentControl? _svgControl;
    private RenderedScore? _renderedScore;

    public static readonly StyledProperty<NotationScore?> ScoreProperty =
        AvaloniaProperty.Register<NotationScoreControl, NotationScore?>(nameof(Score));

    public static readonly StyledProperty<double> StaffSpaceProperty =
        AvaloniaProperty.Register<NotationScoreControl, double>(nameof(StaffSpace), 10.0);

    public static readonly StyledProperty<bool> RenderDebugArtifactsProperty =
        AvaloniaProperty.Register<NotationScoreControl, bool>(nameof(RenderDebugArtifacts), false);

    public static readonly StyledProperty<TimeSpan?> HighlightStartTimeProperty =
        AvaloniaProperty.Register<NotationScoreControl, TimeSpan?>(nameof(HighlightStartTime));

    public static readonly StyledProperty<TimeSpan?> HighlightEndTimeProperty =
        AvaloniaProperty.Register<NotationScoreControl, TimeSpan?>(nameof(HighlightEndTime));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<NotationScoreControl, IBrush?>(nameof(Foreground), Brushes.Black);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<NotationScoreControl, IBrush?>(nameof(Background), Brushes.White);

    public static readonly StyledProperty<IBrush?> HighlightProperty =
        AvaloniaProperty.Register<NotationScoreControl, IBrush?>(nameof(Highlight), Brushes.Red);

    /// <summary>
    /// Gets or sets the notation score to render.
    /// </summary>
    public NotationScore? Score
    {
        get => GetValue(ScoreProperty);
        set => SetValue(ScoreProperty, value);
    }

    /// <summary>
    /// Gets or sets the staff space (pixels between staff lines). Default is 10.0.
    /// </summary>
    public double StaffSpace
    {
        get => GetValue(StaffSpaceProperty);
        set => SetValue(StaffSpaceProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to render debug artifacts. Default is false.
    /// </summary>
    public bool RenderDebugArtifacts
    {
        get => GetValue(RenderDebugArtifactsProperty);
        set => SetValue(RenderDebugArtifactsProperty, value);
    }

    /// <summary>
    /// Gets or sets the start time for highlighting.
    /// </summary>
    public TimeSpan? HighlightStartTime
    {
        get => GetValue(HighlightStartTimeProperty);
        set => SetValue(HighlightStartTimeProperty, value);
    }

    /// <summary>
    /// Gets or sets the end time for highlighting.
    /// If null, only notes active at HighlightStartTime will be highlighted.
    /// </summary>
    public TimeSpan? HighlightEndTime
    {
        get => GetValue(HighlightEndTimeProperty);
        set => SetValue(HighlightEndTimeProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground color for notation elements.
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the highlight color for highlighted notes. Default is red.
    /// </summary>
    public IBrush? Highlight
    {
        get => GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    static NotationScoreControl()
    {
        AffectsRender<NotationScoreControl>(
            ScoreProperty, StaffSpaceProperty, RenderDebugArtifactsProperty,
            HighlightStartTimeProperty, HighlightEndTimeProperty,
            ForegroundProperty, BackgroundProperty, HighlightProperty);

        AffectsMeasure<NotationScoreControl>(ScoreProperty, StaffSpaceProperty);
    }

    public NotationScoreControl()
    {
        _svgControl = new SvgContentControl();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);

        base.OnPropertyChanged(change);

        if (change.Property == ScoreProperty ||
            change.Property == StaffSpaceProperty ||
            change.Property == RenderDebugArtifactsProperty ||
            change.Property == HighlightStartTimeProperty ||
            change.Property == HighlightEndTimeProperty ||
            change.Property == ForegroundProperty ||
            change.Property == BackgroundProperty ||
            change.Property == HighlightProperty)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Score == null || _svgControl == null)
        {
            return new Size(0, 0);
        }

        Render(availableSize.Width);

        _svgControl.Measure(availableSize);
        return _svgControl.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_svgControl != null)
        {
            Render(finalSize.Width);
            _svgControl.Arrange(new Rect(finalSize));
        }
        return finalSize;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        _renderedScore = null;

        base.OnSizeChanged(e);

        InvalidateMeasure();
    }

    private void Render(double availableWidth)
    {
        if (Score == null || _svgControl == null)
        {
            return;
        }

        if (availableWidth <= 0 || double.IsInfinity(availableWidth) || double.IsNaN(availableWidth))
        {
            return;
        }

        var staffSpace = (int)StaffSpace;

        var rerendered = false;
        if (_renderedScore is null)
        {
            var options = new SvgRenderOptions
            {
                MaxWidth = (int)availableWidth,
                StaffSpace = staffSpace,
                RenderDebugArtifacts = RenderDebugArtifacts,
                Foreground = BrushToHexColor(Foreground ?? Brushes.Black),
                Background = BrushToHexColor(Background ?? Brushes.White)
            };

            _renderedScore = SvgScoreExporter.Export(Score, options);
            rerendered = true;
        }

        var highlightColor = BrushToHexColor(Highlight ?? Brushes.Red);
        if (rerendered || _renderedScore.Highlight(HighlightStartTime, HighlightEndTime, highlightColor))
        {
            // The actual XElement instance may not have changed, so we need to clear and reset the SvgContent
            _svgControl.SvgContent = null;
            _svgControl.SvgContent = _renderedScore.SvgRoot;
        }
    }

    private static string BrushToHexColor(IBrush? brush)
    {
        if (brush is ISolidColorBrush solidBrush)
        {
            var color = solidBrush.Color;
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        return "black";
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _svgControl?.Render(context);
    }

    public void Dispose()
    {
        _svgControl?.Dispose();
    }
}
