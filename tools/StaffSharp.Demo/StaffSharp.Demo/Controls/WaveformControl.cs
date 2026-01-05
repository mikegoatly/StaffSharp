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

        // Calculate samples per pixel
        var samplesPerPixel = samples.Length / width;
        var centerY2 = height / 2;
        var amplitude = height / 2 - 2; // Leave some margin

        // Draw waveform
        if (WaveformBrush != null)
        {
            var pen = new Pen(WaveformBrush, 1);

            for (int x = 0; x < (int)width; x++)
            {
                var startSample = (int)(x * samplesPerPixel);
                var endSample = Math.Min((int)((x + 1) * samplesPerPixel), samples.Length);

                if (startSample >= samples.Length)
                {
                    break;
                }

                // Find min/max in this pixel column
                float min = 0, max = 0;
                for (int i = startSample; i < endSample; i++)
                {
                    var sample = samples.Span[i];
                    if (sample < min) min = sample;
                    if (sample > max) max = sample;
                }

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
