namespace StaffSharp.Audio;

/// <summary>
/// Represents an in-memory audio buffer with normalized float samples.
/// </summary>
public sealed class AudioBuffer
{
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
    /// Converts stereo to mono by averaging channels.
    /// Returns the same buffer if already mono.
    /// </summary>
    public AudioBuffer ToMono()
    {
        if (Channels == 1)
            return this;

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
