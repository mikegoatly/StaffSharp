using StaffSharp.Audio.Numerics;
using StaffSharp.Audio.Pipeline;

namespace StaffSharp.Audio.Analysis.Boundaries;

/// <summary>
/// Detects audio boundaries using RMS energy analysis.
/// Scans from start and end to find where audio energy exceeds a threshold.
/// </summary>
public sealed class EnergyBasedBoundaryDetector : IAudioBoundaryDetector
{
    private readonly float _thresholdDb;
    private readonly int _windowSize;
    private readonly int _minContentSamples;

    public EnergyBasedBoundaryDetector(BoundaryDetectionOptions? options = null)
    {
        options ??= new BoundaryDetectionOptions();
        options.Validate();

        _thresholdDb = options.ThresholdDb;
        _windowSize = options.WindowSize;
        _minContentSamples = options.MinContentSamples;
    }

    public AudioBoundaries? DetectBoundaries(PipelineProgress progress, AudioBuffer audio)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(audio);

        // Work with mono for simplicity
        var mono = audio.Channels == 1 ? audio : audio.ToMono();
        var samples = mono.Samples.Span;

        if (samples.Length < _minContentSamples)
        {
            return null; // Too short to contain valid content
        }

        // Convert dB threshold to linear amplitude
        var thresholdLinear = DbToLinear(_thresholdDb);

        // Find start boundary (scan forward)
        if (FindStartBoundary(samples, thresholdLinear) is not { } startSample 
            || FindEndBoundary(samples, thresholdLinear) is not { } endSample
            || (endSample - startSample) < _minContentSamples)
        {
            return null;
        }

        // Calculate silence durations
        var leadingSilence = TimeSpan.FromSeconds(startSample / (double)audio.SampleRate);
        var trailingSilence = TimeSpan.FromSeconds((samples.Length - endSample) / (double)audio.SampleRate);

        var boundaries = new AudioBoundaries(
            StartSample: startSample,
            EndSample: endSample,
            SampleRate: audio.SampleRate,
            LeadingSilence: leadingSilence,
            TrailingSilence: trailingSilence
        );

        progress.EmitDiagnostics("Leading silence", boundaries.LeadingSilence);
        progress.EmitDiagnostics("Trailing silence", boundaries.TrailingSilence);
        progress.EmitDiagnostics("Start sample", boundaries.StartSample);
        progress.EmitDiagnostics("End sample", boundaries.EndSample);
        progress.EmitDiagnostics("Content duration", boundaries.ContentDuration);

        return boundaries;
    }

    /// <summary>
    /// Finds the start of content by scanning forward.
    /// </summary>
    private int? FindStartBoundary(ReadOnlySpan<float> samples, float threshold)
    {
        for (int i = 0; i <= samples.Length - _windowSize; i += _windowSize / 2) // 50% overlap
        {
            var window = samples.Slice(i, _windowSize);
            var rms = SimdOps.ComputeRms(window);

            if (rms >= threshold)
            {
                // Found content! Back up to start of this window
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the end of content by scanning backward.
    /// </summary>
    private int? FindEndBoundary(ReadOnlySpan<float> samples, float threshold)
    {
        // Start from end, work backward
        for (int i = samples.Length - _windowSize; i >= 0; i -= _windowSize / 2) // 50% overlap
        {
            var window = samples.Slice(i, _windowSize);
            var rms = SimdOps.ComputeRms(window);

            if (rms >= threshold)
            {
                // Found content! Forward to end of this window
                return i + _windowSize;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts decibels to linear amplitude.
    /// </summary>
    private static float DbToLinear(float db)
    {
        return MathF.Pow(10.0f, db / 20.0f);
    }
}
