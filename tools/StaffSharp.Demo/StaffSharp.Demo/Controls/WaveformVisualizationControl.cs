using Avalonia;
using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// A control that displays a normalized audio waveform with debug overlays:
/// - Noise floor lines (orange)
/// - Clipping lines at ±1.0 (red)
/// </summary>
public class WaveformVisualizationControl : UserControl
{
    public static readonly StyledProperty<float[]?> SamplesProperty =
        AvaloniaProperty.Register<WaveformVisualizationControl, float[]?>(nameof(Samples));

    public static readonly StyledProperty<int> SampleRateProperty =
        AvaloniaProperty.Register<WaveformVisualizationControl, int>(nameof(SampleRate), 16000);

    public static readonly StyledProperty<bool> ShowNoiseFloorProperty =
        AvaloniaProperty.Register<WaveformVisualizationControl, bool>(nameof(ShowNoiseFloor), true);

    public static readonly StyledProperty<double> NoiseFloorDbProperty =
        AvaloniaProperty.Register<WaveformVisualizationControl, double>(nameof(NoiseFloorDb), -50.0);

    public static readonly StyledProperty<double> PlaybackPositionProperty =
        AvaloniaProperty.Register<WaveformVisualizationControl, double>(nameof(PlaybackPosition), 0.0);

    private readonly AvaPlot _plot;
    private ScottPlot.Plottables.VerticalLine? _playbackMarker;

    public float[]? Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public int SampleRate
    {
        get => GetValue(SampleRateProperty);
        set => SetValue(SampleRateProperty, value);
    }

    public bool ShowNoiseFloor
    {
        get => GetValue(ShowNoiseFloorProperty);
        set => SetValue(ShowNoiseFloorProperty, value);
    }

    public double NoiseFloorDb
    {
        get => GetValue(NoiseFloorDbProperty);
        set => SetValue(NoiseFloorDbProperty, value);
    }

    public double PlaybackPosition
    {
        get => GetValue(PlaybackPositionProperty);
        set => SetValue(PlaybackPositionProperty, value);
    }

    public WaveformVisualizationControl()
    {
        _plot = new AvaPlot();
        Content = _plot;
    }

    static WaveformVisualizationControl()
    {
        SamplesProperty.Changed.AddClassHandler<WaveformVisualizationControl>((x, _) => x.UpdatePlot());
        SampleRateProperty.Changed.AddClassHandler<WaveformVisualizationControl>((x, _) => x.UpdatePlot());
        ShowNoiseFloorProperty.Changed.AddClassHandler<WaveformVisualizationControl>((x, _) => x.UpdatePlot());
        NoiseFloorDbProperty.Changed.AddClassHandler<WaveformVisualizationControl>((x, _) => x.UpdatePlot());
        PlaybackPositionProperty.Changed.AddClassHandler<WaveformVisualizationControl>((x, _) => x.UpdatePlaybackMarker());
    }

    private void UpdatePlot()
    {
        _plot.Plot.Clear();

        if (Samples == null || Samples.Length == 0)
        {
            _plot.Refresh();
            return;
        }

        // Convert float[] to double[] for ScottPlot
        var samplesDouble = new double[Samples.Length];
        for (int i = 0; i < Samples.Length; i++)
        {
            samplesDouble[i] = Samples[i];
        }

        // Add signal plot
        var signal = _plot.Plot.Add.Signal(samplesDouble, 1.0 / SampleRate);
        signal.Color = Colors.DodgerBlue;
        signal.LineWidth = 1;

        // Add debug lines
        if (ShowNoiseFloor)
        {
            var noiseFloorLinear = Math.Pow(10, NoiseFloorDb / 20.0);
            var noiseFloorUpper = _plot.Plot.Add.HorizontalLine(noiseFloorLinear);
            noiseFloorUpper.Color = Colors.Orange;
            noiseFloorUpper.LineWidth = 2;
            noiseFloorUpper.LinePattern = LinePattern.Dashed;

            var noiseFloorLower = _plot.Plot.Add.HorizontalLine(-noiseFloorLinear);
            noiseFloorLower.Color = Colors.Orange;
            noiseFloorLower.LineWidth = 2;
            noiseFloorLower.LinePattern = LinePattern.Dashed;
        }

        // Clipping lines at ±1.0
        var clipUpper = _plot.Plot.Add.HorizontalLine(1.0);
        clipUpper.Color = Colors.Red;
        clipUpper.LineWidth = 1;
        clipUpper.LinePattern = LinePattern.Dotted;

        var clipLower = _plot.Plot.Add.HorizontalLine(-1.0);
        clipLower.Color = Colors.Red;
        clipLower.LineWidth = 1;
        clipLower.LinePattern = LinePattern.Dotted;

        // Configure axes
        _plot.Plot.XLabel("Time (seconds)");
        _plot.Plot.YLabel("Amplitude");
        _plot.Plot.Title("Normalized Waveform");

        // Set default view to first 15 seconds (or full duration if shorter)
        // Note: Pan/zoom is enabled by default in ScottPlot.Avalonia
        var duration = Samples.Length / (double)SampleRate;
        var defaultViewDuration = Math.Min(15.0, duration);
        _plot.Plot.Axes.SetLimits(0, defaultViewDuration, -1.1, 1.1);

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
        if (_playbackMarker == null || Samples == null || Samples.Length == 0)
            return;

        var duration = Samples.Length / (double)SampleRate;
        var currentTime = PlaybackPosition * duration;

        _playbackMarker.X = currentTime;
        _playbackMarker.IsVisible = PlaybackPosition > 0;

        _plot.Refresh();
    }
}
