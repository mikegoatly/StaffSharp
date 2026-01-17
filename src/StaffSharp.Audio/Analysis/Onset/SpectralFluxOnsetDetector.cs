using System.Numerics;
using System.Numerics.Tensors;

using MathNet.Numerics.IntegralTransforms;

using StaffSharp.Audio.Numerics;
using StaffSharp.Audio.Pipeline;

namespace StaffSharp.Audio.Analysis.Onset;

/// <summary>
/// Spectral flux onset detector.
/// Detects onsets by measuring sudden increases in spectral energy.
/// Robust for most instruments and unaffected by pitch harmonics.
/// </summary>
public sealed class SpectralFluxOnsetDetector : IOnsetDetector
{
    private const float MinThresholdFloor = 0.0001f;
    private readonly int _hopSize;
    private readonly int _frameSize;
    private readonly float _threshold;
    private readonly float _minOnsetIntervalSeconds;
    private readonly bool _applyLogarithmicCompression;

    public SpectralFluxOnsetDetector(OnsetDetectionOptions? options = null)
    {
        options ??= new OnsetDetectionOptions();
        options.Validate();

        _frameSize = options.FrameSize;
        _hopSize = options.HopSize;
        _threshold = options.Threshold;
        _minOnsetIntervalSeconds = options.MinOnsetIntervalSeconds;
        _applyLogarithmicCompression = options.ApplyLogarithmicCompression;
    }

    public double[] DetectOnsets(PipelineProgress progress, ReadOnlySpan<float> buffer, int sampleRate, TimeSpan startTimeOffset = default)
    {
        ArgumentNullException.ThrowIfNull(progress);

        progress.ReportProgress("Detecting onsets");

        if (buffer.Length < _frameSize)
        {
            progress.EmitDiagnostics("Buffer too short for onset detection", buffer.Length);
            return [];
        }

        // Step 1: Compute spectral flux for each frame
        var fluxValues = ComputeSpectralFlux(buffer);
        progress.EmitDiagnostics("Flux frames computed", fluxValues.Length);

        // Step 2: Peak picking with threshold
        var onsetFrames = PickPeaks(fluxValues, _threshold);
        progress.EmitDiagnostics("Peaks before filtering", onsetFrames.Count);
        progress.EmitDiagnostics("Threshold", _threshold);
        progress.EmitDiagnostics("Min onset interval (seconds)", _minOnsetIntervalSeconds);

        // Step 3: Convert frame indices to time (seconds)
        var minOnsetIntervalFrames = (int)(_minOnsetIntervalSeconds * sampleRate / _hopSize);
        progress.EmitDiagnostics("Min onset interval (frames)", minOnsetIntervalFrames);
        var onsets = ConvertFramesToTime(onsetFrames, _hopSize, sampleRate, minOnsetIntervalFrames);
        progress.EmitDiagnostics("Onsets after time filtering", onsets.Length);

        // Step 4: Apply start time offset to preserve absolute timing
        if (startTimeOffset != default)
        {
            TensorPrimitives.Add(onsets, startTimeOffset.TotalSeconds, onsets);
        }

        progress.EmitDiagnostics("Onsets", onsets);

        return onsets;
    }

    /// <summary>
    /// Computes spectral flux: the sum of positive differences in spectral magnitude between consecutive frames.
    /// </summary>
    private float[] ComputeSpectralFlux(ReadOnlySpan<float> buffer)
    {
        var frameCount = ((buffer.Length - _frameSize) / _hopSize) + 1;
        var fluxValues = new float[frameCount];
        var window = WindowFunctions.CreateHannWindow(_frameSize);

        // Pre-allocate buffers for the loop
        var windowedFrame = new float[_frameSize];
        var complexBuffer = new Complex[_frameSize];
        var magnitudeSize = (_frameSize / 2) + 1; // Include DC and Nyquist
        var currentMagnitudes = new float[magnitudeSize];
        var prevMagnitudes = new float[magnitudeSize];
        var differences = new float[magnitudeSize];

        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            var frameStart = frameIndex * _hopSize;

            // 1. Copy samples and apply window
            buffer.Slice(frameStart, _frameSize).CopyTo(windowedFrame);
            SimdOps.ApplyWindow(windowedFrame, window);

            // 2. Prepare complex buffer directly
            for (int i = 0; i < _frameSize; i++)
            {
                // Re-using the buffer saves allocations
                complexBuffer[i] = new Complex(windowedFrame[i], 0);
            }

            // 3. Compute FFT in-place
            Fourier.Forward(complexBuffer, FourierOptions.Default);

            // 4. Compute magnitudes
            for (int i = 0; i < magnitudeSize; i++)
            {
                float mag = (float)complexBuffer[i].Magnitude;

                // Apply logarithmic compression if enabled:
                // use log(1 + mag) instead of log(mag) so that zero magnitudes are handled
                // gracefully (no log(0)), and to improve numerical stability for very small values.
                if (_applyLogarithmicCompression)
                {
                    mag = MathF.Log(1 + mag);
                }

                currentMagnitudes[i] = mag;
            }

            // 5. Compute flux: sum of positive differences compared to previous frame
            if (frameIndex > 0)
            {
                // Compute differences: currentMagnitudes - prevMagnitudes
                TensorPrimitives.Subtract(currentMagnitudes, prevMagnitudes, differences);

                // Sum only positive differences (Half-Wave Rectification + Sum)
                // Replace negatives with zeros, then sum
                TensorPrimitives.MaxNumber(differences, 0f, differences);

                fluxValues[frameIndex] = TensorPrimitives.Sum(differences);
            }

            // Swap buffers for next iteration (avoid copy)
            (currentMagnitudes, prevMagnitudes) = (prevMagnitudes, currentMagnitudes);
        }

        return fluxValues;
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

        // Prevent threshold from becoming too sensitive in very quiet passages.
        // This avoids noise triggering in silence but may miss legitimate quiet onsets.
        medianValue = Math.Max(medianValue, MinThresholdFloor);

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
        {
            return [];
        }

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

        return [.. onsets];
    }
}
