using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using StaffSharp.Audio.Numerics;
using System.Numerics;
using System.Numerics.Tensors;

namespace StaffSharp.Audio.Analysis.Onset;

/// <summary>
/// Spectral flux onset detector.
/// Detects onsets by measuring sudden increases in spectral energy.
/// Robust for most instruments and unaffected by pitch harmonics.
/// </summary>
public sealed class SpectralFluxOnsetDetector : IOnsetDetector
{
    private readonly int _hopSize;
    private readonly int _frameSize;
    private readonly float _threshold;
    private readonly float _minOnsetIntervalSeconds;

    public SpectralFluxOnsetDetector(
        int frameSize = 2048,
        int hopSize = 512,
        float threshold = 0.3f,
        float minOnsetIntervalSeconds = 0.05f)
    {
        if (frameSize <= 0 || (frameSize & (frameSize - 1)) != 0)
            throw new ArgumentException("Frame size must be a power of 2", nameof(frameSize));
        if (hopSize <= 0 || hopSize > frameSize)
            throw new ArgumentOutOfRangeException(nameof(hopSize), "Hop size must be positive and <= frame size");
        if (threshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be positive");
        if (minOnsetIntervalSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(minOnsetIntervalSeconds), "Minimum interval must be non-negative");

        _frameSize = frameSize;
        _hopSize = hopSize;
        _threshold = threshold;
        _minOnsetIntervalSeconds = minOnsetIntervalSeconds;
    }

    public double[] DetectOnsets(ReadOnlySpan<float> buffer, int sampleRate, double startTimeOffset = 0.0)
    {
        if (buffer.Length < _frameSize)
            return Array.Empty<double>();

        // Step 1: Compute spectral flux for each frame
        var fluxValues = ComputeSpectralFlux(buffer);

        // Step 2: Peak picking with threshold
        var onsetFrames = PickPeaks(fluxValues, _threshold);

        // Step 3: Convert frame indices to time (seconds)
        var minOnsetIntervalFrames = (int)(_minOnsetIntervalSeconds * sampleRate / _hopSize);
        var onsets = ConvertFramesToTime(onsetFrames, _hopSize, sampleRate, minOnsetIntervalFrames);

        // Step 4: Apply start time offset to preserve absolute timing
        if (startTimeOffset != 0.0)
        {
            TensorPrimitives.Add(onsets, startTimeOffset, onsets);
        }

        return onsets;
    }

    /// <summary>
    /// Computes spectral flux: the sum of positive differences in spectral magnitude between consecutive frames.
    /// </summary>
    private float[] ComputeSpectralFlux(ReadOnlySpan<float> buffer)
    {
        var frameCount = (buffer.Length - _frameSize) / _hopSize + 1;
        var fluxValues = new float[frameCount];
        var window = WindowFunctions.CreateHannWindow(_frameSize);

        float[]? prevMagnitudes = null;

        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            var frameStart = frameIndex * _hopSize;
            var frame = buffer.Slice(frameStart, _frameSize);

            // Apply window and compute FFT
            var windowedFrame = new float[_frameSize];
            frame.CopyTo(windowedFrame);
            SimdOps.ApplyWindow(windowedFrame, window);

            var magnitudes = ComputeMagnitudeSpectrum(windowedFrame);

            // Compute flux: sum of positive differences
            if (prevMagnitudes != null)
            {
                float flux = 0;
                for (int i = 0; i < magnitudes.Length; i++)
                {
                    var diff = magnitudes[i] - prevMagnitudes[i];
                    if (diff > 0)
                        flux += diff;
                }
                fluxValues[frameIndex] = flux;
            }

            prevMagnitudes = magnitudes;
        }

        return fluxValues;
    }

    /// <summary>
    /// Computes magnitude spectrum from audio frame using FFT.
    /// </summary>
    private static float[] ComputeMagnitudeSpectrum(float[] frame)
    {
        // Convert to complex for FFT
        var complexFrame = new Complex[frame.Length];
        for (int i = 0; i < frame.Length; i++)
        {
            complexFrame[i] = new Complex(frame[i], 0);
        }

        // Perform FFT
        Fourier.Forward(complexFrame, FourierOptions.Default);

        // Compute magnitude (only need first half due to symmetry)
        var magnitudes = new float[complexFrame.Length / 2];
        for (int i = 0; i < magnitudes.Length; i++)
        {
            magnitudes[i] = (float)complexFrame[i].Magnitude;
        }

        return magnitudes;
    }

    /// <summary>
    /// Picks peaks in the flux function that exceed the threshold.
    /// </summary>
    private static List<int> PickPeaks(float[] values, float threshold)
    {
        var peaks = new List<int>();

        // Adaptive threshold using median
        var sortedValues = values.Where(v => v > 0).OrderBy(v => v).ToArray();
        var medianValue = sortedValues.Length > 0 ? sortedValues[sortedValues.Length / 2] : 0;
        var adaptiveThreshold = medianValue * threshold;

        for (int i = 1; i < values.Length - 1; i++)
        {
            // Check if this is a local maximum above threshold
            if (values[i] > adaptiveThreshold &&
                values[i] > values[i - 1] &&
                values[i] >= values[i + 1])
            {
                peaks.Add(i);
            }
        }

        return peaks;
    }

    /// <summary>
    /// Converts frame indices to time in seconds and applies minimum interval filter.
    /// </summary>
    private static double[] ConvertFramesToTime(List<int> frames, int hopSize, int sampleRate, int minIntervalFrames)
    {
        if (frames.Count == 0)
            return Array.Empty<double>();

        var onsets = new List<double>();
        int lastFrame = -minIntervalFrames;

        foreach (var frame in frames)
        {
            // Enforce minimum interval between onsets
            if (frame - lastFrame >= minIntervalFrames)
            {
                var timeSeconds = (double)(frame * hopSize) / sampleRate;
                onsets.Add(timeSeconds);
                lastFrame = frame;
            }
        }

        return onsets.ToArray();
    }
}
