using StaffSharp.Audio;
using StaffSharp.Audio.IO;

namespace StaffSharp.Audio.Tests.IO;

public class WavReaderTests
{
    [Fact]
    public async Task ReadAsync_Valid16BitMonoWav_ReadsCorrectly()
    {
        // Create a simple 16-bit mono WAV in memory
        var wavData = CreateSimple16BitWav(sampleRate: 44100, durationSeconds: 0.1, frequency: 440);

        using var stream = new MemoryStream(wavData);
        var buffer = await WavReader.ReadAsync(stream);

        Assert.Equal(44100, buffer.SampleRate);
        Assert.Equal(1, buffer.Channels);
        Assert.Equal(0.1, buffer.DurationSeconds, precision: 2);
    }

    [Fact]
    public async Task ReadAsync_InvalidRiffHeader_ThrowsException()
    {
        var invalidData = "JUNK"u8.ToArray();
        using var stream = new MemoryStream(invalidData);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => WavReader.ReadAsync(stream));
    }

    [Fact]
    public async Task ReadAsync_MissingDataChunk_ThrowsException()
    {
        // Create WAV with fmt but no data chunk
        var wavData = CreateWavWithoutDataChunk();
        using var stream = new MemoryStream(wavData);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => WavReader.ReadAsync(stream));
    }

    // Helper: Creates a minimal valid 16-bit WAV file
    private static byte[] CreateSimple16BitWav(int sampleRate, double durationSeconds, double frequency)
    {
        var sampleCount = (int)(sampleRate * durationSeconds);
        var samples = new short[sampleCount];

        // Generate simple sine wave
        for (int i = 0; i < sampleCount; i++)
        {
            var t = i / (double)sampleRate;
            samples[i] = (short)(Math.Sin(2 * Math.PI * frequency * t) * 16000);
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + sampleCount * 2); // File size - 8
        writer.Write("WAVE"u8.ToArray());

        // fmt chunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // PCM
        writer.Write((short)1); // Mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2); // Byte rate
        writer.Write((short)2); // Block align
        writer.Write((short)16); // Bits per sample

        // data chunk
        writer.Write("data"u8.ToArray());
        writer.Write(sampleCount * 2);
        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        return ms.ToArray();
    }

    private static byte[] CreateWavWithoutDataChunk()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(44100);
        writer.Write(88200);
        writer.Write((short)2);
        writer.Write((short)16);

        return ms.ToArray();
    }
}
