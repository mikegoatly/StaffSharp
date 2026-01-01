using StaffSharp.Audio;
using StaffSharp.Audio.IO;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.IO;

public class WavReaderTests
{
    [Fact]
    public async Task ReadAsync_Valid16BitMonoWav_ReadsCorrectly()
    {
        // Create a simple 16-bit mono WAV in memory
        using var stream = WavStreamBuilder.CreateSineWave(sampleRate: 44100, durationSeconds: 0.1, frequency: 440);
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
