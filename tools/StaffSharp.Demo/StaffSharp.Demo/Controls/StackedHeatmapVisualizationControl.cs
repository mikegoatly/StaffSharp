using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// A control that displays the full ML transcription pipeline in a 2x2 grid:
/// - Top Left: Onset probabilities
/// - Top Right: Frame probabilities
/// - Bottom Left: Offset probabilities
/// - Bottom Right: Piano roll (decoded notes)
/// All aligned on the same time axis with shared crosshair for easy comparison.
/// </summary>
public class StackedHeatmapVisualizationControl : UserControl
{
    public static readonly StyledProperty<float[,]?> OnsetDataProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, float[,]?>(nameof(OnsetData));

    public static readonly StyledProperty<float[,]?> FrameDataProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, float[,]?>(nameof(FrameData));

    public static readonly StyledProperty<float[,]?> OffsetDataProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, float[,]?>(nameof(OffsetData));

    public static readonly StyledProperty<double> FrameRateProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, double>(nameof(FrameRate), 100.0);

    public static readonly StyledProperty<IReadOnlyList<StaffSharp.NoteEvent>?> NoteEventsProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, IReadOnlyList<StaffSharp.NoteEvent>?>(nameof(NoteEvents));

    public static readonly StyledProperty<bool> ShowVelocityProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, bool>(nameof(ShowVelocity), true);

    public static readonly StyledProperty<double> PlaybackPositionProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, double>(nameof(PlaybackPosition), 0.0);

    private readonly AvaPlot _onsetPlot;
    private readonly AvaPlot _framePlot;
    private readonly AvaPlot _offsetPlot;
    private readonly AvaPlot _pianoRollPlot;

    private ScottPlot.Plottables.Crosshair? _onsetCrosshair;
    private ScottPlot.Plottables.Crosshair? _frameCrosshair;
    private ScottPlot.Plottables.Crosshair? _offsetCrosshair;
    private ScottPlot.Plottables.Crosshair? _pianoRollCrosshair;

    private ScottPlot.Plottables.VerticalLine? _onsetPlaybackMarker;
    private ScottPlot.Plottables.VerticalLine? _framePlaybackMarker;
    private ScottPlot.Plottables.VerticalLine? _offsetPlaybackMarker;
    private ScottPlot.Plottables.VerticalLine? _pianoRollPlaybackMarker;

    public float[,]? OnsetData
    {
        get => GetValue(OnsetDataProperty);
        set => SetValue(OnsetDataProperty, value);
    }

    public float[,]? FrameData
    {
        get => GetValue(FrameDataProperty);
        set => SetValue(FrameDataProperty, value);
    }

    public float[,]? OffsetData
    {
        get => GetValue(OffsetDataProperty);
        set => SetValue(OffsetDataProperty, value);
    }

    public double FrameRate
    {
        get => GetValue(FrameRateProperty);
        set => SetValue(FrameRateProperty, value);
    }

    public IReadOnlyList<StaffSharp.NoteEvent>? NoteEvents
    {
        get => GetValue(NoteEventsProperty);
        set => SetValue(NoteEventsProperty, value);
    }

    public bool ShowVelocity
    {
        get => GetValue(ShowVelocityProperty);
        set => SetValue(ShowVelocityProperty, value);
    }

    public double PlaybackPosition
    {
        get => GetValue(PlaybackPositionProperty);
        set => SetValue(PlaybackPositionProperty, value);
    }

    public StackedHeatmapVisualizationControl()
    {
        _onsetPlot = new AvaPlot();
        _framePlot = new AvaPlot();
        _offsetPlot = new AvaPlot();
        _pianoRollPlot = new AvaPlot();

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,*"), // 2 equal rows
            ColumnDefinitions = new ColumnDefinitions("*,*") // 2 equal columns
        };

        // Top row: Onset (left), Frame (right)
        Grid.SetRow(_onsetPlot, 0);
        Grid.SetColumn(_onsetPlot, 0);
        Grid.SetRow(_framePlot, 0);
        Grid.SetColumn(_framePlot, 1);

        // Bottom row: Offset (left), Piano Roll (right)
        Grid.SetRow(_offsetPlot, 1);
        Grid.SetColumn(_offsetPlot, 0);
        Grid.SetRow(_pianoRollPlot, 1);
        Grid.SetColumn(_pianoRollPlot, 1);

        grid.Children.Add(_onsetPlot);
        grid.Children.Add(_framePlot);
        grid.Children.Add(_offsetPlot);
        grid.Children.Add(_pianoRollPlot);

        Content = grid;
    }

    static StackedHeatmapVisualizationControl()
    {
        OnsetDataProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlots());
        FrameDataProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlots());
        OffsetDataProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlots());
        FrameRateProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlots());
        NoteEventsProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlots());
        ShowVelocityProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlots());
        PlaybackPositionProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlaybackMarkers());
    }

    private void UpdatePlots()
    {
        _onsetPlot.Plot.Clear();
        _framePlot.Plot.Clear();
        _offsetPlot.Plot.Clear();
        _pianoRollPlot.Plot.Clear();

        if (OnsetData == null || FrameData == null || OffsetData == null)
        {
            _onsetPlot.Refresh();
            _framePlot.Refresh();
            _offsetPlot.Refresh();
            _pianoRollPlot.Refresh();
            return;
        }

        // Data comes as [timeFrames, keys] but ScottPlot heatmaps use [rows, cols] where rows=Y, cols=X
        // We need to transpose: [keys, timeFrames] so keys map to Y-axis and time to X-axis
        var timeFrames = OnsetData.GetLength(0);
        var keys = OnsetData.GetLength(1); // Should be 88
        
        // Transpose: swap dimensions so time is on X-axis, notes on Y-axis
        var onsetDouble = new double[keys, timeFrames];  // [notes/Y, time/X]
        for (int t = 0; t < timeFrames; t++)
        {
            for (int k = 0; k < keys; k++)
            {
                onsetDouble[k, t] = OnsetData[t, k];  // Transpose: swap indices
            }
        }

        // Transpose frame data the same way
        var frameDouble = new double[keys, timeFrames];  // [notes/Y, time/X]
        for (int t = 0; t < timeFrames; t++)
        {
            for (int k = 0; k < keys; k++)
            {
                frameDouble[k, t] = FrameData[t, k];  // Transpose: swap indices
            }
        }

        // Transpose offset data the same way
        var offsetDouble = new double[keys, timeFrames];  // [notes/Y, time/X]
        for (int t = 0; t < timeFrames; t++)
        {
            for (int k = 0; k < keys; k++)
            {
                offsetDouble[k, t] = OffsetData[t, k];  // Transpose: swap indices
            }
        }

        var duration = timeFrames / FrameRate;

        // Left plot: Onset probabilities
        var onsetHeatmap = _onsetPlot.Plot.Add.Heatmap(onsetDouble);
        onsetHeatmap.Colormap = new ScottPlot.Colormaps.Magma();
        //onsetHeatmap.Interpolation = ScottPlot.Image.Interpolation.NearestNeighbor; // Prevent blurring
        // Map heatmap to time in seconds (X: 0 to duration, Y: MIDI 109-21)
        // Y is reversed (109 to 21) so low notes are at bottom, high notes at top
        // Data resolution: timeFrames columns × 88 rows mapped to duration seconds × 88 notes
        onsetHeatmap.Extent = new CoordinateRect(0, duration, 109, 21);
        _onsetPlot.Plot.Title("Onset Probabilities");
        _onsetPlot.Plot.YLabel("Note");
        _onsetPlot.Plot.XLabel("Time (seconds)");
        AddMusicalNoteGridLines(_onsetPlot.Plot);
        SetNoteNameTicks(_onsetPlot.Plot);

        // Middle plot: Frame probabilities  
        var frameHeatmap = _framePlot.Plot.Add.Heatmap(frameDouble);
        frameHeatmap.Colormap = new ScottPlot.Colormaps.Magma();
        //frameHeatmap.Interpolation = ScottPlot.Image.Interpolation.NearestNeighbor; // Prevent blurring
        // Map heatmap to time in seconds (X: 0 to duration, Y: MIDI 109-21)
        // Y is reversed (109 to 21) so low notes are at bottom, high notes at top
        // Data resolution: timeFrames columns × 88 rows mapped to duration seconds × 88 notes
        frameHeatmap.Extent = new CoordinateRect(0, duration, 109, 21);
        _framePlot.Plot.Title("Frame Probabilities");
        _framePlot.Plot.YLabel("Note");
        _framePlot.Plot.XLabel("Time (seconds)");
        AddMusicalNoteGridLines(_framePlot.Plot);
        SetNoteNameTicks(_framePlot.Plot);

        // Bottom left plot: Offset probabilities
        var offsetHeatmap = _offsetPlot.Plot.Add.Heatmap(offsetDouble);
        offsetHeatmap.Colormap = new ScottPlot.Colormaps.Magma();
        //offsetHeatmap.Interpolation = ScottPlot.Image.Interpolation.NearestNeighbor; // Prevent blurring
        // Y is reversed (109 to 21) so low notes are at bottom, high notes at top
        offsetHeatmap.Extent = new CoordinateRect(0, duration, 109, 21);
        _offsetPlot.Plot.Title("Offset Probabilities");
        _offsetPlot.Plot.YLabel("Note");
        _offsetPlot.Plot.XLabel("Time (seconds)");
        AddMusicalNoteGridLines(_offsetPlot.Plot);
        SetNoteNameTicks(_offsetPlot.Plot);

        // Bottom right plot: Piano roll (decoded notes)
        _pianoRollPlot.Plot.Title("Piano Roll (Decoded)");
        _pianoRollPlot.Plot.YLabel("Note");
        _pianoRollPlot.Plot.XLabel("Time (seconds)");
        AddMusicalNoteGridLines(_pianoRollPlot.Plot);
        SetNoteNameTicks(_pianoRollPlot.Plot);

        if (NoteEvents != null && NoteEvents.Count > 0)
        {
            foreach (var note in NoteEvents)
            {
                var x1 = note.Onset.TotalSeconds;
                var x2 = x1 + note.Duration.TotalSeconds;
                var y1 = note.Pitch.Value;
                var y2 = y1 + 0.8; // Leave small gap between notes

                // Use opacity for velocity: loud notes = solid, soft notes = transparent
                var baseColor = Colors.DodgerBlue;
                var color = ShowVelocity
                    ? baseColor.WithAlpha(note.Velocity.Value)
                    : baseColor;

                var rect = _pianoRollPlot.Plot.Add.Rectangle(x1, x2, y1, y2);
                rect.FillColor = color;
                rect.LineWidth = 0;
            }
        }

        // Set default view to first 15 seconds (or full duration if shorter)
        // All plots now use the same coordinate system (time in seconds)
        var defaultViewDuration = Math.Min(15.0, duration);
        _onsetPlot.Plot.Axes.SetLimits(0, defaultViewDuration, 21, 109);
        _framePlot.Plot.Axes.SetLimits(0, defaultViewDuration, 21, 109);
        _offsetPlot.Plot.Axes.SetLimits(0, defaultViewDuration, 21, 109);
        _pianoRollPlot.Plot.Axes.SetLimits(0, defaultViewDuration, 21, 109);

        // Synchronize both X and Y axes across all 4 plots
        _onsetPlot.Plot.Axes.Link(_framePlot, x: true, y: true);
        _onsetPlot.Plot.Axes.Link(_offsetPlot, x: true, y: true);
        _onsetPlot.Plot.Axes.Link(_pianoRollPlot, x: true, y: true);
        _framePlot.Plot.Axes.Link(_onsetPlot, x: true, y: true);
        _framePlot.Plot.Axes.Link(_offsetPlot, x: true, y: true);
        _framePlot.Plot.Axes.Link(_pianoRollPlot, x: true, y: true);
        _offsetPlot.Plot.Axes.Link(_onsetPlot, x: true, y: true);
        _offsetPlot.Plot.Axes.Link(_framePlot, x: true, y: true);
        _offsetPlot.Plot.Axes.Link(_pianoRollPlot, x: true, y: true);
        _pianoRollPlot.Plot.Axes.Link(_onsetPlot, x: true, y: false);
        _pianoRollPlot.Plot.Axes.Link(_framePlot, x: true, y: false);
        _pianoRollPlot.Plot.Axes.Link(_offsetPlot, x: true, y: false);

        // Add crosshairs (initially hidden)
        _onsetCrosshair = _onsetPlot.Plot.Add.Crosshair(0, 60);
        _onsetCrosshair.IsVisible = false;

        _frameCrosshair = _framePlot.Plot.Add.Crosshair(0, 60);
        _frameCrosshair.IsVisible = false;

        _offsetCrosshair = _offsetPlot.Plot.Add.Crosshair(0, 60);
        _offsetCrosshair.IsVisible = false;

        _pianoRollCrosshair = _pianoRollPlot.Plot.Add.Crosshair(0, 60);
        _pianoRollCrosshair.IsVisible = false;

        // Wire up mouse events for synchronized crosshair
        _onsetPlot.PointerMoved += OnPlotPointerMoved;
        _framePlot.PointerMoved += OnPlotPointerMoved;
        _offsetPlot.PointerMoved += OnPlotPointerMoved;
        _pianoRollPlot.PointerMoved += OnPlotPointerMoved;

        _onsetPlot.PointerExited += (s, e) => HideAllCrosshairs();
        _framePlot.PointerExited += (s, e) => HideAllCrosshairs();
        _offsetPlot.PointerExited += (s, e) => HideAllCrosshairs();
        _pianoRollPlot.PointerExited += (s, e) => HideAllCrosshairs();

        // Add playback markers (initially hidden)
        _onsetPlaybackMarker = _onsetPlot.Plot.Add.VerticalLine(0);
        _onsetPlaybackMarker.Color = Colors.Red;
        _onsetPlaybackMarker.LineWidth = 2;
        _onsetPlaybackMarker.IsVisible = false;

        _framePlaybackMarker = _framePlot.Plot.Add.VerticalLine(0);
        _framePlaybackMarker.Color = Colors.Red;
        _framePlaybackMarker.LineWidth = 2;
        _framePlaybackMarker.IsVisible = false;

        _offsetPlaybackMarker = _offsetPlot.Plot.Add.VerticalLine(0);
        _offsetPlaybackMarker.Color = Colors.Red;
        _offsetPlaybackMarker.LineWidth = 2;
        _offsetPlaybackMarker.IsVisible = false;

        _pianoRollPlaybackMarker = _pianoRollPlot.Plot.Add.VerticalLine(0);
        _pianoRollPlaybackMarker.Color = Colors.Red;
        _pianoRollPlaybackMarker.LineWidth = 2;
        _pianoRollPlaybackMarker.IsVisible = false;

        UpdatePlaybackMarkers();

        _onsetPlot.Refresh();
        _framePlot.Refresh();
        _offsetPlot.Refresh();
        _pianoRollPlot.Refresh();
    }

    private void OnPlotPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var plot = sender as AvaPlot;
        if (plot == null || _onsetCrosshair == null) return;

        var position = e.GetPosition(plot);
        var coords = plot.Plot.GetCoordinates((float)position.X, (float)position.Y);

        // Update all crosshairs to the same position
        _onsetCrosshair.IsVisible = true;
        _onsetCrosshair.Position = coords;
        
        _frameCrosshair!.IsVisible = true;
        _frameCrosshair.Position = coords;
        
        _offsetCrosshair!.IsVisible = true;
        _offsetCrosshair.Position = coords;
        
        _pianoRollCrosshair!.IsVisible = true;
        _pianoRollCrosshair.Position = coords;

        // Refresh all plots
        _onsetPlot.Refresh();
        _framePlot.Refresh();
        _offsetPlot.Refresh();
        _pianoRollPlot.Refresh();
    }

    private void HideAllCrosshairs()
    {
        if (_onsetCrosshair != null) _onsetCrosshair.IsVisible = false;
        if (_frameCrosshair != null) _frameCrosshair.IsVisible = false;
        if (_offsetCrosshair != null) _offsetCrosshair.IsVisible = false;
        if (_pianoRollCrosshair != null) _pianoRollCrosshair.IsVisible = false;
        
        _onsetPlot.Refresh();
        _framePlot.Refresh();
        _offsetPlot.Refresh();
        _pianoRollPlot.Refresh();
    }

    private void UpdatePlaybackMarkers()
    {
        if (_onsetPlaybackMarker == null || OnsetData == null)
            return;

        var timeFrames = OnsetData.GetLength(0);
        var duration = timeFrames / FrameRate;
        var currentTime = PlaybackPosition * duration;

        _onsetPlaybackMarker.X = currentTime;
        _onsetPlaybackMarker.IsVisible = PlaybackPosition > 0;

        _framePlaybackMarker!.X = currentTime;
        _framePlaybackMarker.IsVisible = PlaybackPosition > 0;

        _offsetPlaybackMarker!.X = currentTime;
        _offsetPlaybackMarker.IsVisible = PlaybackPosition > 0;

        _pianoRollPlaybackMarker!.X = currentTime;
        _pianoRollPlaybackMarker.IsVisible = PlaybackPosition > 0;

        _onsetPlot.Refresh();
        _framePlot.Refresh();
        _offsetPlot.Refresh();
        _pianoRollPlot.Refresh();
    }

    private static void AddMusicalNoteGridLines(Plot plot)
    {
        // Add horizontal grid lines at every C note (octaves)
        // MIDI notes: C1=24, C2=36, C3=48, C4=60 (middle C), C5=72, C6=84, C7=96, C8=108
        int[] cNotes = [24, 36, 48, 60, 72, 84, 96, 108];
        string[] labels = ["C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8"];

        for (int i = 0; i < cNotes.Length; i++)
        {
            var line = plot.Add.HorizontalLine(cNotes[i]);
            line.Color = Colors.Gray.WithAlpha(0.3);
            line.LineWidth = 1;
            line.LinePattern = LinePattern.Dotted;
        }
    }

    private static void SetNoteNameTicks(Plot plot)
    {
        // Set Y-axis tick labels to show note names (C, C#, D, etc.) at octave markers
        // MIDI notes: C1=24, C2=36, C3=48, C4=60 (middle C), C5=72, C6=84, C7=96, C8=108
        double[] tickPositions = [24, 36, 48, 60, 72, 84, 96, 108];
        string[] tickLabels = ["C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8"];

        plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickPositions, tickLabels);
    }
}
