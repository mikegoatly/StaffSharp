using System.Numerics.Tensors;

namespace StaffSharp.Audio;

/// <summary>
/// Represents an in-memory audio buffer with normalized float samples.
/// </summary>
public sealed class AudioBuffer
{
    private const float Minus120Db = 1e-6f;

    public AudioBuffer(float[] samples, int sampleRate, int channels = 1)
    {
        ArgumentNullException.ThrowIfNull(samples);
        Samples = samples;
        SampleRate = sampleRate > 0 ? sampleRate : throw new ArgumentOutOfRangeException(nameof(sampleRate));
        Channels = channels > 0 ? channels : throw new ArgumentOutOfRangeException(nameof(channels));
    }

    /// <summary>
    /// Audio samples, normalized to [-1.0, 1.0] range.
    /// For stereo/multi-channel: interleaved (L, R, L, R, ...).
    /// Use .Span for efficient access in audio processing operations.
    /// </summary>
    public ReadOnlyMemory<float> Samples { get; }

    /// <summary>
    /// Sample rate in Hz (e.g., 44100, 48000).
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// Number of channels (1=mono, 2=stereo).
    /// </summary>
    public int Channels { get; }

    /// <summary>
    /// Total duration of audio in seconds.
    /// </summary>
    public double DurationSeconds => (double)Samples.Length / (SampleRate * Channels);

    /// <summary>
    /// Number of samples (total across all channels).
    /// </summary>
    public int SampleCount => Samples.Length;

    public void Save(Stream stream)
    {
        using var writer = new BinaryWriter(stream);

        int bitsPerSample = 16;
        int bytesPerSample = bitsPerSample / 8;
        int byteRate = SampleRate * Channels * bytesPerSample;
        int blockAlign = Channels * bytesPerSample;
        int dataSize = Samples.Length * bytesPerSample;

        // RIFF header
        writer.Write(['R', 'I', 'F', 'F']);
        writer.Write(36 + dataSize);
        writer.Write(['W', 'A', 'V', 'E']);

        // fmt subchunk
        writer.Write(['f', 'm', 't', ' ']);
        writer.Write(16); // Subchunk1Size for PCM
        writer.Write((short)1); // AudioFormat (PCM)
        writer.Write((short)Channels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data subchunk
        writer.Write(['d', 'a', 't', 'a']);
        writer.Write(dataSize);

        // Convert float samples to 16-bit PCM
        foreach (var sample in Samples.Span)
        {
            var clampedSample = Math.Clamp(sample, -1.0f, 1.0f);
            var pcmSample = (short)(clampedSample * short.MaxValue);
            writer.Write(pcmSample);
        }
    }

    /// <summary>
    /// Normalizes the audio volume so the peak amplitude matches the target.
    /// </summary>
    /// <param name="targetAmplitude">Target peak amplitude.</param>
    /// <param name="minAllowedPeak">Minimum allowed peak amplitude before normalization.</param>
    /// <param name="maxAllowedPeak">Maximum allowed peak amplitude before normalization.</param>
    /// <returns>A new normalized AudioBuffer, or the original if no change is needed.</returns>
    public (AudioBuffer, NormalizationStats) Normalize(float targetAmplitude = 0.6f, float minAllowedPeak = 0.4f, float maxAllowedPeak = 0.85f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(targetAmplitude, 0f);

        var span = Samples.Span;
        
        // Find peak amplitude
        float max = TensorPrimitives.MaxMagnitude(span);
        float absMax = Math.Abs(max);

        // If silent or between allowed ranges, return original
        // 1e-4f is roughly 0.01% tolerance
        if (absMax <= Minus120Db || (absMax >= minAllowedPeak - 1e-4f && absMax <= maxAllowedPeak + 1e-4f))
        {
            return (this, new NormalizationStats(absMax, 0f));
        }

        float gain = targetAmplitude / absMax;
        var newSamples = new float[Samples.Length];
        
        // Apply gain
        TensorPrimitives.Multiply(span, gain, newSamples);

        return (
            new AudioBuffer(newSamples, SampleRate, Channels),
            new NormalizationStats(OriginalPeakAmplitude: absMax, GainApplied: gain));
    }

    /// <summary>
    /// Converts stereo to mono by averaging channels.
    /// Returns the same buffer if already mono.
    /// </summary>
    public AudioBuffer ToMono()
    {
        if (Channels == 1)
        {
            return this;
        }

        var samplesSpan = Samples.Span;
        var monoSamples = new float[Samples.Length / Channels];

        float multiplier = 1.0f / Channels;

        // Optimization: Handle Stereo (2 channels) explicitly
        // This is 99% of use cases and allows the CPU to unroll the loop better
        if (Channels == 2)
        {
            for (int i = 0; i < monoSamples.Length; i++)
            {
                // Direct access avoids inner loop overhead
                var offset = i * 2;
                float left = samplesSpan[offset];
                float right = samplesSpan[offset + 1];
                monoSamples[i] = (left + right) * 0.5f;
            }
        }
        else
        {
            // General case for 3+ channels
            for (int i = 0; i < monoSamples.Length; i++)
            {
                float sum = 0;
                for (int ch = 0; ch < Channels; ch++)
                {
                    sum += samplesSpan[i * Channels + ch];
                }

                monoSamples[i] = sum * multiplier;
            }
        }

        return new AudioBuffer(monoSamples, SampleRate, 1);
    }

    /// <summary>
    /// Resamples the audio to a target sample rate using linear interpolation.
    /// Returns the same buffer if already at the target sample rate.
    /// </summary>
    /// <param name="targetSampleRate">The desired sample rate in Hz.</param>
    /// <returns>A new resampled AudioBuffer, or the original if no change is needed.</returns>
    public AudioBuffer Resample(int targetSampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(targetSampleRate, 0);

        if (SampleRate == targetSampleRate)
        {
            return this;
        }

        var samples = Samples.ToArray();
        var ratio = (double)targetSampleRate / SampleRate;
        var newLength = (int)(samples.Length * ratio);
        var resampled = new float[newLength];

        // Simple linear interpolation resampling
        for (int i = 0; i < newLength; i++)
        {
            var srcIndex = i / ratio;
            var srcIndexInt = (int)srcIndex;
            var frac = (float)(srcIndex - srcIndexInt);

            if (srcIndexInt + 1 < samples.Length)
            {
                resampled[i] = (samples[srcIndexInt] * (1 - frac)) + (samples[srcIndexInt + 1] * frac);
            }
            else if (srcIndexInt < samples.Length)
            {
                resampled[i] = samples[srcIndexInt];
            }
        }

        return new AudioBuffer(resampled, targetSampleRate, Channels);
    }

    /// <summary>
    /// Detects the start and end of audio content by finding regions above a silence threshold.
    /// Uses frame-based RMS analysis for stability.
    /// </summary>
    /// <param name="silenceThresholdDb">Threshold in dB below which audio is considered silence.</param>
    /// <param name="frameSize">Size of analysis frames in samples.</param>
    /// <param name="hopSize">Hop size between frames in samples.</param>
    /// <returns>Start and end times of detected content, or (TimeSpan.Zero, TotalDuration) if no silence detected.</returns>
    public (TimeSpan Start, TimeSpan End) DetectContent(float silenceThresholdDb = -45.0f, int frameSize = 1024, int hopSize = 128)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(silenceThresholdDb, 0f);

        // Convert to mono if needed for analysis
        AudioBuffer monoAudio = Channels > 1 ? ToMono() : this;
        var monoSamples = monoAudio.Samples.Span;

        // Convert dB threshold to linear amplitude
        var linearThreshold = MathF.Pow(10, silenceThresholdDb / 20.0f);

        // Calculate number of frames
        var numFrames = Math.Max(1, ((monoSamples.Length - frameSize) / hopSize) + 1);

        // Find first non-silent frame
        int? firstContentFrame = null;
        for (int frameIdx = 0; frameIdx < numFrames; frameIdx++)
        {
            var frameStart = frameIdx * hopSize;
            var frameEnd = Math.Min(frameStart + frameSize, monoSamples.Length);
            var frameSlice = monoSamples.Slice(frameStart, frameEnd - frameStart);

            // Calculate RMS for this frame
            var rms = CalculateRms(frameSlice);

            if (rms > linearThreshold)
            {
                firstContentFrame = frameIdx;
                break;
            }
        }

        // Find last non-silent frame (search backwards)
        int? lastContentFrame = null;
        for (int frameIdx = numFrames - 1; frameIdx >= 0; frameIdx--)
        {
            var frameStart = frameIdx * hopSize;
            var frameEnd = Math.Min(frameStart + frameSize, monoSamples.Length);
            var frameSlice = monoSamples.Slice(frameStart, frameEnd - frameStart);

            // Calculate RMS for this frame
            var rms = CalculateRms(frameSlice);

            if (rms > linearThreshold)
            {
                lastContentFrame = frameIdx;
                break;
            }
        }

        // Convert frame indices to time
        if (!firstContentFrame.HasValue || !lastContentFrame.HasValue)
        {
            // No content detected or all content - return full duration
            return (TimeSpan.Zero, TimeSpan.FromSeconds(DurationSeconds));
        }

        var samplesPerChannel = monoSamples.Length;
        var startSample = firstContentFrame.Value * hopSize;
        var endSample = Math.Min((lastContentFrame.Value * hopSize) + frameSize, samplesPerChannel);

        var startTime = TimeSpan.FromSeconds((double)startSample / SampleRate);
        var endTime = TimeSpan.FromSeconds((double)endSample / SampleRate);

        return (startTime, endTime);
    }

    private int CalculateSampleIndex(TimeSpan time)
    {
        return Math.Clamp((int)(time.TotalSeconds * SampleRate) * Channels, 0, Samples.Length);
    }

    public AudioBuffer MuteOutsideRange(TimeSpan startTime, TimeSpan endTime)
    {
        var samples = Samples.Span;
        var newSamples = samples.ToArray();

        // Convert time range to sample indices
        var startSample = CalculateSampleIndex(startTime);
        var endSample = CalculateSampleIndex(endTime);

        if (endSample < startSample)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        if (startSample == 0 && endSample >= newSamples.Length)
        {
            // No muting needed
            return this;
        }

        if (startSample > 0)
        {
            Array.Fill(newSamples, 0f, 0, startSample);
        }

        if (endSample > 0)
        {
            Array.Fill(newSamples, 0f, endSample, newSamples.Length - endSample);
        }

        return new AudioBuffer(newSamples, SampleRate, Channels);
    }

    /// <summary>
    /// Normalizes audio based on RMS (Root Mean Square) level instead of peak amplitude.
    /// Optionally normalizes only within a specific time range.
    /// </summary>
    /// <param name="targetRms">Target RMS level.</param>
    /// <returns>A new normalized AudioBuffer with RMS-based normalization applied.</returns>
    public (AudioBuffer, RmsNormalizationStats) NormalizeRms(float targetRms = 0.1f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(targetRms, 0.0f, nameof(targetRms));

        var samples = Samples.Span;

        // Calculate RMS of the region
        var rms = CalculateRms(samples);

        // If silent, return original
        if (rms <= Minus120Db)
        {
            return (this, new RmsNormalizationStats(rms, 1.0f));
        }

        // Calculate gain
        var gain = targetRms / rms;

        // Apply gain to create new buffer
        var newSamples = samples.ToArray();
        TensorPrimitives.Multiply(newSamples, gain, newSamples);

        return (
            new AudioBuffer(newSamples, SampleRate, Channels),
            new RmsNormalizationStats(rms, gain));
    }

    /// <summary>
    /// Calculates the RMS (Root Mean Square) of a sample buffer.
    /// </summary>
    private static float CalculateRms(ReadOnlySpan<float> samples)
    {
        if (samples.Length == 0)
        {
            return 0;
        }

        float sumOfSquares = TensorPrimitives.SumOfSquares(samples);
        return MathF.Sqrt(sumOfSquares / samples.Length);
    }
}


public readonly record struct NormalizationStats(float OriginalPeakAmplitude, float GainApplied);
public readonly record struct RmsNormalizationStats(float OriginalRms, float GainApplied);