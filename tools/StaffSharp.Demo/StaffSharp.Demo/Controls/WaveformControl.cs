using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// A control that displays an audio waveform visualization.
/// </summary>
public class WaveformControl : Control
{
    public static readonly StyledProperty<ReadOnlyMemory<float>?> SamplesProperty =
        AvaloniaProperty.Register<WaveformControl, ReadOnlyMemory<float>?>(nameof(Samples));

    public static readonly StyledProperty<IBrush?> WaveformBrushProperty =
        AvaloniaProperty.Register<WaveformControl, IBrush?>(nameof(WaveformBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<WaveformControl, IBrush?>(nameof(BackgroundBrush), Brushes.Transparent);

    public static readonly StyledProperty<double> PlaybackPositionProperty =
        AvaloniaProperty.Register<WaveformControl, double>(nameof(PlaybackPosition), 0.0);

    public static readonly StyledProperty<IBrush?> PlayheadBrushProperty =
        AvaloniaProperty.Register<WaveformControl, IBrush?>(nameof(PlayheadBrush), Brushes.Red);

    private ReadOnlyMemory<float>? _cachedSamples;
    private (float Min, float Max)[]? _downsampledData;
    private int _downsampledWidth;

    public ReadOnlyMemory<float>? Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public IBrush? WaveformBrush
    {
        get => GetValue(WaveformBrushProperty);
        set => SetValue(WaveformBrushProperty, value);
    }

    public IBrush? BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    /// <summary>
    /// Playback position as a value from 0.0 to 1.0.
    /// </summary>
    public double PlaybackPosition
    {
        get => GetValue(PlaybackPositionProperty);
        set => SetValue(PlaybackPositionProperty, value);
    }

    public IBrush? PlayheadBrush
    {
        get => GetValue(PlayheadBrushProperty);
        set => SetValue(PlayheadBrushProperty, value);
    }

    static WaveformControl()
    {
        AffectsRender<WaveformControl>(SamplesProperty, WaveformBrushProperty, BackgroundBrushProperty, PlaybackPositionProperty, PlayheadBrushProperty);
    }

    private void UpdateDownsampledData(ReadOnlyMemory<float> samples, int targetWidth)
    {
        if (_cachedSamples.HasValue && 
            _cachedSamples.Value.Length == samples.Length &&
            _downsampledWidth == targetWidth &&
            _downsampledData != null)
        {
            return;
        }

        _cachedSamples = samples;
        _downsampledWidth = targetWidth;
        _downsampledData = new (float Min, float Max)[targetWidth];

        var samplesPerPixel = (double)samples.Length / targetWidth;
        var span = samples.Span;

        for (int x = 0; x < targetWidth; x++)
        {
            var startSample = (int)(x * samplesPerPixel);
            var endSample = Math.Min((int)((x + 1) * samplesPerPixel), samples.Length);

            if (startSample >= samples.Length)
            {
                break;
            }

            float min = 0, max = 0;
            for (int i = startSample; i < endSample; i++)
            {
                var sample = span[i];
                if (sample < min) min = sample;
                if (sample > max) max = sample;
            }

            _downsampledData[x] = (min, max);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var width = bounds.Width;
        var height = bounds.Height;

        // Draw background
        if (BackgroundBrush != null)
        {
            context.FillRectangle(BackgroundBrush, new Rect(0, 0, width, height));
        }

        if (Samples is not { Length: > 0 } samples || width <= 0 || height <= 0)
        {
            // Draw placeholder
            var pen = new Pen(Brushes.Gray, 1);
            var centerY = height / 2;
            context.DrawLine(pen, new Point(0, centerY), new Point(width, centerY));
            return;
        }

        var targetWidth = (int)width;
        UpdateDownsampledData(samples, targetWidth);

        var centerY2 = height / 2;
        var amplitude = height / 2 - 2; // Leave some margin

        // Draw waveform
        if (WaveformBrush != null && _downsampledData != null)
        {
            var pen = new Pen(WaveformBrush, 1);

            for (int x = 0; x < Math.Min(targetWidth, _downsampledData.Length); x++)
            {
                var (min, max) = _downsampledData[x];

                // Draw vertical line from min to max
                var y1 = centerY2 - max * amplitude;
                var y2 = centerY2 - min * amplitude;

                // Ensure at least 1 pixel height
                if (Math.Abs(y2 - y1) < 1)
                {
                    y1 = centerY2 - 0.5;
                    y2 = centerY2 + 0.5;
                }

                context.DrawLine(pen, new Point(x, y1), new Point(x, y2));
            }
        }

        // Draw playhead
        if (PlaybackPosition > 0 && PlayheadBrush != null)
        {
            var playheadX = PlaybackPosition * width;
            var playheadPen = new Pen(PlayheadBrush, 2);
            context.DrawLine(playheadPen, new Point(playheadX, 0), new Point(playheadX, height));
        }
    }
}
