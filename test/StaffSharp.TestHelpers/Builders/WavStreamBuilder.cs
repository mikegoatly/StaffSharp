using StaffSharp.Audio;

namespace StaffSharp.TestHelpers.Builders;

/// <summary>
/// Builder for creating WAV file streams from audio samples for testing.
/// </summary>
public static class WavStreamBuilder
{
    /// <summary>
    /// Creates a WAV stream from an AudioBuffer.
    /// </summary>
    public static MemoryStream FromAudioBuffer(AudioBuffer audio)
    {
        var stream = new MemoryStream();
        WriteWavFile(stream, audio.Samples.Span, audio.SampleRate, audio.Channels);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a WAV stream from float samples.
    /// </summary>
    public static MemoryStream FromSamples(ReadOnlySpan<float> samples, int sampleRate, int channels = 1)
    {
        var stream = new MemoryStream();
        WriteWavFile(stream, samples, sampleRate, channels);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a simple 16-bit mono WAV with a sine wave.
    /// </summary>
    public static MemoryStream CreateSineWave(int sampleRate, double durationSeconds, double frequency, double amplitude = 0.5)
    {
        var samples = AudioSignalBuilder.Sine(frequency, durationSeconds, sampleRate, amplitude);
        return FromSamples(samples, sampleRate, channels: 1);
    }

    private static void WriteWavFile(Stream stream, ReadOnlySpan<float> samples, int sampleRate, int channels)
    {
        var bitsPerSample = 16;
        var byteRate = sampleRate * channels * (bitsPerSample / 8);
        var blockAlign = (short)(channels * (bitsPerSample / 8));
        var dataSize = samples.Length * sizeof(short);

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // RIFF header
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize); // File size - 8
        writer.Write("WAVE"u8.ToArray());

        // fmt chunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // PCM format
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        // Convert float samples to 16-bit PCM
        for (int i = 0; i < samples.Length; i++)
        {
            var sample = Math.Clamp(samples[i], -1.0f, 1.0f);
            var pcm16 = (short)(sample * short.MaxValue);
            writer.Write(pcm16);
        }
    }
}
