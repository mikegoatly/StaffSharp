using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;

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

    public static readonly StyledProperty<double> PlaybackPercentageProperty =
        AvaloniaProperty.Register<StackedHeatmapVisualizationControl, double>(nameof(PlaybackPercentage), 0.0);

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

    public double PlaybackPercentage
    {
        get => GetValue(PlaybackPercentageProperty);
        set => SetValue(PlaybackPercentageProperty, value);
    }

    public StackedHeatmapVisualizationControl()
    {
        _onsetPlot = new AvaPlot();
        _framePlot = new AvaPlot();
        _offsetPlot = new AvaPlot();
        _pianoRollPlot = new AvaPlot();

        var grid = new UniformGrid
        {
            Columns = 4
        };

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
        PlaybackPercentageProperty.Changed.AddClassHandler<StackedHeatmapVisualizationControl>((x, _) => x.UpdatePlaybackMarkers());
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

        // Transpose: swap dimensions and cast as a doulbe so time is on X-axis, notes on Y-axis
        var onsetDouble = OnsetData.SwapDimensionsToDouble();
        var frameDouble = FrameData.SwapDimensionsToDouble();
        var offsetDouble = OffsetData.SwapDimensionsToDouble();

        var duration = timeFrames / FrameRate;

        // Left plot: Onset probabilities
        var onsetHeatmap = _onsetPlot.Plot.Add.Heatmap(onsetDouble);
        _onsetPlot.Plot.ConfigurePianoRollHeatmap(onsetHeatmap, "Onset Probabilities", duration);

        // Middle plot: Frame probabilities  
        var frameHeatmap = _framePlot.Plot.Add.Heatmap(frameDouble);
        _framePlot.Plot.ConfigurePianoRollHeatmap(frameHeatmap, "Frame Probabilities", duration);

        // Bottom left plot: Offset probabilities
        var offsetHeatmap = _offsetPlot.Plot.Add.Heatmap(offsetDouble);
        _offsetPlot.Plot.ConfigurePianoRollHeatmap(offsetHeatmap, "Offset Probabilities", duration);

        // Bottom right plot: Piano roll (decoded notes)
        _pianoRollPlot.Plot.Title("Piano Roll (Decoded)");
        _pianoRollPlot.Plot.YLabel("Note");
        _pianoRollPlot.Plot.AddMusicalNoteGridLines();
        _pianoRollPlot.Plot.SetMusicalNoteTickLabels();

        if (NoteEvents != null && NoteEvents.Count > 0)
        {
            foreach (var note in NoteEvents)
            {
                var x1 = note.Onset.TotalSeconds;
                var x2 = x1 + note.Duration.TotalSeconds;
                var y1 = note.Pitch.Value;
                var y2 = y1 + 0.8; // Leave small gap between notes

                // Use opacity for velocity: loud notes = solid, soft notes = transparent
                var baseColor = Colors.DarkBlue;
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
        SynchronizePlots(_onsetPlot, _framePlot, _offsetPlot, _pianoRollPlot);

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

    private static void SynchronizePlots(params AvaPlot[] plots)
    {
        foreach (var plot in plots)
        {
            foreach (var syncWith in plots.Where(p => p != plot))
            {
                syncWith.Plot.Axes.Link(plot, x: true, y: true);
            }
        }
    }

    private void OnPlotPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not AvaPlot plot || _onsetCrosshair is null)
        {
            return;
        }

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
        if (_onsetCrosshair != null)
        {
            _onsetCrosshair.IsVisible = false;
        }

        if (_frameCrosshair != null)
        {
            _frameCrosshair.IsVisible = false;
        }

        if (_offsetCrosshair != null)
        {
            _offsetCrosshair.IsVisible = false;
        }

        if (_pianoRollCrosshair != null)
        {
            _pianoRollCrosshair.IsVisible = false;
        }

        _onsetPlot.Refresh();
        _framePlot.Refresh();
        _offsetPlot.Refresh();
        _pianoRollPlot.Refresh();
    }

    private void UpdatePlaybackMarkers()
    {
        if (_onsetPlaybackMarker == null || OnsetData == null)
        {
            return;
        }

        var timeFrames = OnsetData.GetLength(0);
        var duration = timeFrames / FrameRate;
        var currentTime = PlaybackPercentage * duration;

        _onsetPlaybackMarker.X = currentTime;
        _onsetPlaybackMarker.IsVisible = PlaybackPercentage > 0;

        _framePlaybackMarker!.X = currentTime;
        _framePlaybackMarker.IsVisible = PlaybackPercentage > 0;

        _offsetPlaybackMarker!.X = currentTime;
        _offsetPlaybackMarker.IsVisible = PlaybackPercentage > 0;

        _pianoRollPlaybackMarker!.X = currentTime;
        _pianoRollPlaybackMarker.IsVisible = PlaybackPercentage > 0;

        _onsetPlot.Refresh();
        _framePlot.Refresh();
        _offsetPlot.Refresh();
        _pianoRollPlot.Refresh();
    }
}
