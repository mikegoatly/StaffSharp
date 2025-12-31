using StaffSharp.Audio.Numerics;

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

    /// <summary>
    /// Creates a new energy-based boundary detector.
    /// </summary>
    /// <param name="thresholdDb">Energy threshold in dB (e.g., -40 dB). Levels below this are considered silence.</param>
    /// <param name="windowSize">Window size in samples for energy calculation. Default: 2048 (~46ms at 44.1kHz).</param>
    /// <param name="minContentSamples">Minimum number of samples for valid content. Default: 4410 (~100ms at 44.1kHz).</param>
    public EnergyBasedBoundaryDetector(
        float thresholdDb = -40.0f,
        int windowSize = 2048,
        int minContentSamples = 4410)
    {
        if (thresholdDb >= 0)
            throw new ArgumentException("Threshold must be negative (dB)", nameof(thresholdDb));
        if (windowSize <= 0)
            throw new ArgumentException("Window size must be positive", nameof(windowSize));
        if (minContentSamples <= 0)
            throw new ArgumentException("Minimum content samples must be positive", nameof(minContentSamples));

        _thresholdDb = thresholdDb;
        _windowSize = windowSize;
        _minContentSamples = minContentSamples;
    }

    public AudioBoundaries? DetectBoundaries(AudioBuffer audio)
    {
        ArgumentNullException.ThrowIfNull(audio);

        // Work with mono for simplicity
        var mono = audio.Channels == 1 ? audio : audio.ToMono();
        var samples = mono.Samples.Span;

        if (samples.Length < _minContentSamples)
            return null; // Too short to contain valid content

        // Convert dB threshold to linear amplitude
        var thresholdLinear = DbToLinear(_thresholdDb);

        // Find start boundary (scan forward)
        int? startSample = FindStartBoundary(samples, thresholdLinear);
        if (!startSample.HasValue)
            return null; // No content found

        // Find end boundary (scan backward)
        int? endSample = FindEndBoundary(samples, thresholdLinear);
        if (!endSample.HasValue)
            return null; // No content found

        // Validate content length
        var contentLength = endSample.Value - startSample.Value;
        if (contentLength < _minContentSamples)
            return null; // Content too short

        // Calculate silence durations
        var leadingSilence = TimeSpan.FromSeconds(startSample.Value / (double)audio.SampleRate);
        var trailingSilence = TimeSpan.FromSeconds((samples.Length - endSample.Value) / (double)audio.SampleRate);

        return new AudioBoundaries(
            StartSample: startSample.Value,
            EndSample: endSample.Value,
            SampleRate: audio.SampleRate,
            LeadingSilence: leadingSilence,
            TrailingSilence: trailingSilence
        );
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
