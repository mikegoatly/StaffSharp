namespace StaffSharp.Audio.IO;

/// <summary>
/// Reads WAV files (RIFF/WAVE format) and decodes PCM audio to normalized float buffers.
/// Supports 16-bit, 24-bit, and 32-bit PCM formats.
/// No external dependencies - custom RIFF parser.
/// </summary>
public static class WavReader
{
    /// <summary>
    /// Reads a WAV file from a stream.
    /// </summary>
    public static async Task<AudioBuffer> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Read RIFF header
        var riffHeader = reader.ReadBytes(4);
        if (!IsMatch(riffHeader, "RIFF"u8))
            throw new InvalidDataException("Not a valid WAV file: missing RIFF header");

        var fileSize = reader.ReadInt32();

        var waveHeader = reader.ReadBytes(4);
        if (!IsMatch(waveHeader, "WAVE"u8))
            throw new InvalidDataException("Not a valid WAV file: missing WAVE header");

        // Find and read fmt chunk
        WavFormat format = default;
        byte[]? audioData = null;

        while (stream.Position < stream.Length)
        {
            var chunkId = reader.ReadBytes(4);
            var chunkSize = reader.ReadInt32();

            if (IsMatch(chunkId, "fmt "u8))
            {
                format = ReadFormatChunk(reader, chunkSize);
            }
            else if (IsMatch(chunkId, "data"u8))
            {
                audioData = reader.ReadBytes(chunkSize);
                break; // Data chunk is typically last
            }
            else
            {
                // Skip unknown chunks
                stream.Seek(chunkSize, SeekOrigin.Current);
            }
        }

        if (audioData == null)
            throw new InvalidDataException("WAV file has no data chunk");

        // Decode PCM to float
        var samples = DecodePcmToFloat(audioData, format);

        return new AudioBuffer(samples, format.SampleRate, format.Channels);
    }

    private static WavFormat ReadFormatChunk(BinaryReader reader, int chunkSize)
    {
        var audioFormat = reader.ReadInt16(); // 1 = PCM
        var channels = reader.ReadInt16();
        var sampleRate = reader.ReadInt32();
        var byteRate = reader.ReadInt32();
        var blockAlign = reader.ReadInt16();
        var bitsPerSample = reader.ReadInt16();

        // Skip extra format bytes if present
        var extraBytes = chunkSize - 16;
        if (extraBytes > 0)
            reader.ReadBytes(extraBytes);

        if (audioFormat != 1) // 1 = PCM
            throw new NotSupportedException($"Only PCM format is supported (format code: {audioFormat})");

        if (bitsPerSample != 16 && bitsPerSample != 24 && bitsPerSample != 32)
            throw new NotSupportedException($"Only 16, 24, and 32-bit PCM is supported (got {bitsPerSample}-bit)");

        return new WavFormat(channels, sampleRate, bitsPerSample);
    }

    private static float[] DecodePcmToFloat(byte[] data, WavFormat format)
    {
        var bytesPerSample = format.BitsPerSample / 8;
        var sampleCount = data.Length / bytesPerSample;
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            var byteOffset = i * bytesPerSample;

            samples[i] = format.BitsPerSample switch
            {
                16 => Decode16Bit(data, byteOffset),
                24 => Decode24Bit(data, byteOffset),
                32 => Decode32Bit(data, byteOffset),
                _ => throw new InvalidOperationException()
            };
        }

        return samples;
    }

    private static float Decode16Bit(byte[] data, int offset)
    {
        // 16-bit signed PCM: range -32768 to 32767
        var sample = (short)(data[offset] | (data[offset + 1] << 8));
        return sample / 32768f;
    }

    private static float Decode24Bit(byte[] data, int offset)
    {
        // 24-bit signed PCM (little-endian)
        var sample = data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);

        // Sign extend from 24 to 32 bits
        if ((sample & 0x800000) != 0)
            sample |= unchecked((int)0xFF000000);

        return sample / 8388608f; // 2^23
    }

    private static float Decode32Bit(byte[] data, int offset)
    {
        // 32-bit signed PCM
        var sample = data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
        return sample / 2147483648f; // 2^31
    }

    private static bool IsMatch(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> expected)
    {
        return bytes.SequenceEqual(expected);
    }

    private readonly record struct WavFormat(int Channels, int SampleRate, int BitsPerSample);
}
