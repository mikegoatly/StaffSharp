using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.AxisPanels;
using ScottPlot.Plottables;
using StaffSharp.Audio;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// A control that displays multiple waveforms side-by-side with linked panning:
/// - Original waveform
/// - Normalized waveform (if present)
/// - Resampled waveform (if present)
/// All aligned on the same time axis with synchronized panning and zooming.
/// </summary>
public class StackedWaveformVisualizationControl : UserControl
{
    public static readonly StyledProperty<AudioBuffer?> OriginalWaveformProperty =
        AvaloniaProperty.Register<StackedWaveformVisualizationControl, AudioBuffer?>(nameof(OriginalWaveform));

    public static readonly StyledProperty<float[]?> NormalizedWaveformProperty =
        AvaloniaProperty.Register<StackedWaveformVisualizationControl, float[]?>(nameof(NormalizedWaveform));

    public static readonly StyledProperty<float[]?> ResampledWaveformProperty =
        AvaloniaProperty.Register<StackedWaveformVisualizationControl, float[]?>(nameof(ResampledWaveform));

    public static readonly StyledProperty<int> ResampledSampleRateProperty =
        AvaloniaProperty.Register<StackedWaveformVisualizationControl, int>(nameof(ResampledSampleRate), 16000);

    public static readonly StyledProperty<double> PlaybackPercentageProperty =
        AvaloniaProperty.Register<StackedWaveformVisualizationControl, double>(nameof(PlaybackPercentage), 0.0);

    private readonly AvaPlot _originalPlot;
    private readonly AvaPlot _normalizedPlot;
    private readonly AvaPlot _resampledPlot;
    private readonly UniformGrid _grid;

    private VerticalLine? _originalPlaybackMarker;
    private VerticalLine? _normalizedPlaybackMarker;
    private VerticalLine? _resampledPlaybackMarker;

    public AudioBuffer? OriginalWaveform
    {
        get => GetValue(OriginalWaveformProperty);
        set => SetValue(OriginalWaveformProperty, value);
    }

    public float[]? NormalizedWaveform
    {
        get => GetValue(NormalizedWaveformProperty);
        set => SetValue(NormalizedWaveformProperty, value);
    }

    public float[]? ResampledWaveform
    {
        get => GetValue(ResampledWaveformProperty);
        set => SetValue(ResampledWaveformProperty, value);
    }

    public int ResampledSampleRate
    {
        get => GetValue(ResampledSampleRateProperty);
        set => SetValue(ResampledSampleRateProperty, value);
    }

    public double PlaybackPercentage
    {
        get => GetValue(PlaybackPercentageProperty);
        set => SetValue(PlaybackPercentageProperty, value);
    }

    public StackedWaveformVisualizationControl()
    {
        _originalPlot = new AvaPlot();
        _normalizedPlot = new AvaPlot();
        _resampledPlot = new AvaPlot();

        _grid = new UniformGrid
        {
            Columns = 1
        };

        _grid.Children.Add(_originalPlot);

        Content = _grid;
    }

    static StackedWaveformVisualizationControl()
    {
        OriginalWaveformProperty.Changed.AddClassHandler<StackedWaveformVisualizationControl>((x, _) => x.UpdatePlots());
        NormalizedWaveformProperty.Changed.AddClassHandler<StackedWaveformVisualizationControl>((x, _) => x.UpdatePlots());
        ResampledWaveformProperty.Changed.AddClassHandler<StackedWaveformVisualizationControl>((x, _) => x.UpdatePlots());
        ResampledSampleRateProperty.Changed.AddClassHandler<StackedWaveformVisualizationControl>((x, _) => x.UpdatePlots());
        PlaybackPercentageProperty.Changed.AddClassHandler<StackedWaveformVisualizationControl>((x, _) => x.UpdatePlaybackMarkers());
    }

    private void UpdatePlots()
    {
        _originalPlot.Plot.Clear();
        _normalizedPlot.Plot.Clear();
        _resampledPlot.Plot.Clear();

        // Update grid layout based on which plots have data
        _grid.Children.Clear();
        int columnCount = 0;

        if (OriginalWaveform == null)
        {
            _originalPlot.Refresh();
            _normalizedPlot.Refresh();
            _resampledPlot.Refresh();
            return;
        }

        // Always add original plot
        _grid.Children.Add(_originalPlot);
        columnCount++;

        if (NormalizedWaveform != null)
        {
            _grid.Children.Add(_normalizedPlot);
            columnCount++;
        }

        if (ResampledWaveform != null)
        {
            _grid.Children.Add(_resampledPlot);
            columnCount++;
        }

        _grid.Columns = columnCount;

        // Plot original waveform
        var originalSamples = OriginalWaveform.Samples.ToArray();
        _originalPlaybackMarker = PlotWaveform(
            _originalPlot,
            originalSamples,
            OriginalWaveform.SampleRate,
            "Original",
            Colors.DodgerBlue);

        // Plot normalized waveform if present
        if (NormalizedWaveform != null)
        {
            _normalizedPlaybackMarker = PlotWaveform(
                _normalizedPlot,
                NormalizedWaveform,
                OriginalWaveform.SampleRate,
                "Normalized",
                Colors.Green);
        }

        // Plot resampled waveform if present
        if (ResampledWaveform != null)
        {
            _resampledPlaybackMarker = PlotWaveform(
                _resampledPlot,
                ResampledWaveform,
                ResampledSampleRate,
                "Resampled",
                Colors.Orange);
        }

        // Link axes for synchronized panning and zooming
        LinkAxes();

        _originalPlot.Refresh();
        _normalizedPlot.Refresh();
        _resampledPlot.Refresh();
    }

    private static VerticalLine PlotWaveform(AvaPlot plot, float[] samples, int sampleRate, string label, Color waveformColor)
    {
        // Convert float[] to double[] for ScottPlot
        var samplesDouble = new double[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            samplesDouble[i] = samples[i];
        }

        // Add signal plot
        var signal = plot.Plot.Add.Signal(samplesDouble, 1.0 / sampleRate);
        signal.Color = waveformColor;
        signal.LineWidth = 1;

        // Add noise floor lines
        var noiseFloorLinear = Math.Pow(10, -50.0 / 20.0);
        var noiseFloorUpper = plot.Plot.Add.HorizontalLine(noiseFloorLinear);
        noiseFloorUpper.Color = Colors.Orange;
        noiseFloorUpper.LineWidth = 2;
        noiseFloorUpper.LinePattern = LinePattern.Dashed;

        var noiseFloorLower = plot.Plot.Add.HorizontalLine(-noiseFloorLinear);
        noiseFloorLower.Color = Colors.Orange;
        noiseFloorLower.LineWidth = 2;
        noiseFloorLower.LinePattern = LinePattern.Dashed;

        // Clipping lines at ±1.0
        var clipUpper = plot.Plot.Add.HorizontalLine(1.0);
        clipUpper.Color = Colors.Red;
        clipUpper.LineWidth = 1;
        clipUpper.LinePattern = LinePattern.Dotted;

        var clipLower = plot.Plot.Add.HorizontalLine(-1.0);
        clipLower.Color = Colors.Red;
        clipLower.LineWidth = 1;
        clipLower.LinePattern = LinePattern.Dotted;

        plot.Plot.YLabel(label);
        plot.Plot.Axes.SetLimitsY(-1.1, 1.1);

        // Add playback marker
        var playbackMarker = plot.Plot.Add.VerticalLine(0);
        playbackMarker.Color = Colors.Red;
        playbackMarker.LineWidth = 2;
        playbackMarker.IsVisible = false;

        return playbackMarker;
    }

    private void LinkAxes()
    {
        // Collect all plots that have data
        var plots = new List<AvaPlot> { _originalPlot };
        
        if (NormalizedWaveform != null)
        {
            plots.Add(_normalizedPlot);
        }
        
        if (ResampledWaveform != null)
        {
            plots.Add(_resampledPlot);
        }

        // Share X-axis (time) across all plots with data
        SynchronizePlots(plots.ToArray());
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

    private void UpdatePlaybackMarkers()
    {
        if (_originalPlaybackMarker == null || OriginalWaveform == null)
        {
            return;
        }

        var currentTime = PlaybackPercentage * OriginalWaveform.DurationSeconds;

        _originalPlaybackMarker.X = currentTime;
        _originalPlaybackMarker.IsVisible = PlaybackPercentage > 0;

        if (_normalizedPlaybackMarker != null)
        {
            _normalizedPlaybackMarker.X = currentTime;
            _normalizedPlaybackMarker.IsVisible = PlaybackPercentage > 0;
        }

        if (_resampledPlaybackMarker != null)
        {
            _resampledPlaybackMarker.X = currentTime;
            _resampledPlaybackMarker.IsVisible = PlaybackPercentage > 0;
        }

        _originalPlot.Refresh();
        _normalizedPlot.Refresh();
        _resampledPlot.Refresh();
    }
}
