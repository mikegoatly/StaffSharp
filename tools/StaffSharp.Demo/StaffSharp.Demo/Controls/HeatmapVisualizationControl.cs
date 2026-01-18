using System.Linq;
using Avalonia;
using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// A control that displays a single heatmap visualization.
/// Used for displaying mel spectrograms or other 2D time-frequency data.
/// </summary>
public class HeatmapVisualizationControl : UserControl
{
    public static readonly StyledProperty<float[,]?> HeatmapDataProperty =
        AvaloniaProperty.Register<HeatmapVisualizationControl, float[,]?>(nameof(HeatmapData));

    public static readonly StyledProperty<double> FrameRateProperty =
        AvaloniaProperty.Register<HeatmapVisualizationControl, double>(nameof(FrameRate), 100.0);

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<HeatmapVisualizationControl, string?>(nameof(Title));

    public static readonly StyledProperty<string?> YLabelProperty =
        AvaloniaProperty.Register<HeatmapVisualizationControl, string?>(nameof(YLabel), "Frequency Bin");

    public static readonly StyledProperty<double> PlaybackPositionProperty =
        AvaloniaProperty.Register<HeatmapVisualizationControl, double>(nameof(PlaybackPosition), 0.0);

    private readonly AvaPlot _plot;
    private ScottPlot.Plottables.VerticalLine? _playbackMarker;

    public float[,]? HeatmapData
    {
        get => GetValue(HeatmapDataProperty);
        set => SetValue(HeatmapDataProperty, value);
    }

    public double FrameRate
    {
        get => GetValue(FrameRateProperty);
        set => SetValue(FrameRateProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? YLabel
    {
        get => GetValue(YLabelProperty);
        set => SetValue(YLabelProperty, value);
    }

    public double PlaybackPosition
    {
        get => GetValue(PlaybackPositionProperty);
        set => SetValue(PlaybackPositionProperty, value);
    }

    public HeatmapVisualizationControl()
    {
        _plot = new AvaPlot();
        Content = _plot;
    }

    static HeatmapVisualizationControl()
    {
        HeatmapDataProperty.Changed.AddClassHandler<HeatmapVisualizationControl>((x, _) => x.UpdatePlot());
        FrameRateProperty.Changed.AddClassHandler<HeatmapVisualizationControl>((x, _) => x.UpdatePlot());
        TitleProperty.Changed.AddClassHandler<HeatmapVisualizationControl>((x, _) => x.UpdatePlot());
        YLabelProperty.Changed.AddClassHandler<HeatmapVisualizationControl>((x, _) => x.UpdatePlot());
        PlaybackPositionProperty.Changed.AddClassHandler<HeatmapVisualizationControl>((x, _) => x.UpdatePlaybackMarker());
    }

    private void UpdatePlot()
    {
        _plot.Plot.Clear();

        if (HeatmapData == null)
        {
            _plot.Refresh();
            return;
        }

        // Data comes as [timeFrames, bins] but ScottPlot heatmaps use [rows, cols] where rows=Y, cols=X
        // We need to transpose: [bins, timeFrames] so bins map to Y-axis and time to X-axis
        var timeFrames = HeatmapData.GetLength(0);
        var bins = HeatmapData.GetLength(1);
        
        // Transpose: swap dimensions so time is on X-axis, frequency bins on Y-axis
        var heatmapDouble = new double[bins, timeFrames];  // [bins/Y, time/X]

        for (int t = 0; t < timeFrames; t++)
        {
            for (int b = 0; b < bins; b++)
            {
                heatmapDouble[b, t] = HeatmapData[t, b];  // Transpose: swap indices
            }
        }

        var heatmap = _plot.Plot.Add.Heatmap(heatmapDouble);

        // Use grayscale colormap (white background, dark signals) for better visibility
        heatmap.Colormap = new ScottPlot.Colormaps.Magma();
        //heatmap.Interpolation = ScottPlot.Image.Interpolation.NearestNeighbor; // Prevent blurring

        // Scale X-axis to time (seconds)
        var duration = timeFrames / FrameRate;
        // Map: X-axis = 0 to duration (time in seconds), Y-axis = 0 to bins (frequency bins)
        heatmap.Extent = new CoordinateRect(0, duration, 0, bins);

        // Configure axes
        _plot.Plot.XLabel("Time (seconds)");
        _plot.Plot.YLabel(YLabel ?? "Frequency Bin");

        if (!string.IsNullOrEmpty(Title))
        {
            _plot.Plot.Title(Title);
        }

        // Add colorbar
        _plot.Plot.Add.ColorBar(heatmap);

        // Set default view to first 15 seconds (or full duration if shorter)
        var defaultViewDuration = Math.Min(15.0, duration);
        _plot.Plot.Axes.SetLimits(0, defaultViewDuration, 0, bins);

        // Add playback marker
        _playbackMarker = _plot.Plot.Add.VerticalLine(0);
        _playbackMarker.Color = Colors.Red;
        _playbackMarker.LineWidth = 2;
        _playbackMarker.IsVisible = false; // Hidden until playback starts

        UpdatePlaybackMarker();

        _plot.Refresh();
    }

    private void UpdatePlaybackMarker()
    {
        if (_playbackMarker == null || HeatmapData == null)
            return;

        var timeFrames = HeatmapData.GetLength(0);
        var duration = timeFrames / FrameRate;
        var currentTime = PlaybackPosition * duration;

        _playbackMarker.X = currentTime;
        _playbackMarker.IsVisible = PlaybackPosition > 0;

        _plot.Refresh();
    }
}
