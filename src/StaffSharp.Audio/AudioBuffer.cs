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

    /// <summary>
    /// Normalizes the audio volume so the peak amplitude matches the target.
    /// </summary>
    /// <param name="targetAmplitude">Target peak amplitude (default 1.0).</param>
    /// <returns>A new normalized AudioBuffer, or the original if no change is needed.</returns>
    public (AudioBuffer, NormalizationStats) Normalize(float targetAmplitude = 1.0f)
    {
        if (targetAmplitude <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetAmplitude), "Target amplitude must be positive.");
        }

        var span = Samples.Span;
        
        // Find peak amplitude
        float max = TensorPrimitives.MaxMagnitude(span);
        float absMax = Math.Abs(max);

        // If silent or already close to target, return original
        // 1e-4f is roughly 0.01% tolerance
        if (absMax <= Minus120Db || Math.Abs(absMax - targetAmplitude) < 1e-4f)
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

        for (int i = 0; i < monoSamples.Length; i++)
        {
            float sum = 0;
            for (int ch = 0; ch < Channels; ch++)
            {
                sum += samplesSpan[i * Channels + ch];
            }

            monoSamples[i] = sum / Channels;
        }

        return new AudioBuffer(monoSamples, SampleRate, 1);
    }
}


public readonly record struct NormalizationStats(
    float OriginalPeakAmplitude,
    float GainApplied);