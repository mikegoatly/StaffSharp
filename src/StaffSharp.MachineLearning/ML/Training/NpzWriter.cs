namespace StaffSharp.MachineLearning.ML.Training;

using System.IO.Compression;
using System.Text;

/// <summary>
/// Writes training data samples to NumPy .npz format (compressed ZIP of .npy files).
/// This format is compatible with Python's numpy.load().
/// </summary>
public sealed class NpzWriter
{
    private readonly Dictionary<string, object> _entries = new();

    /// <summary>
    /// Adds a 2D float array to the NPZ archive.
    /// </summary>
    /// <param name="name">Name of the array (without .npy extension).</param>
    /// <param name="array">2D float array to store.</param>
    public void AddArray(string name, float[,] array)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(array);

        _entries[name] = array;
    }

    /// <summary>
    /// Adds a string value to the NPZ archive.
    /// </summary>
    /// <param name="name">Name of the value (without .npy extension).</param>
    /// <param name="value">String value to store.</param>
    public void AddString(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        _entries[name] = value;
    }

    /// <summary>
    /// Writes all added data to an NPZ file.
    /// </summary>
    /// <param name="path">Output file path.</param>
    public void Save(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
        try
        {
            using (var zipArchive = ZipFile.Open(tempPath, ZipArchiveMode.Create))
            {
                foreach (var (name, value) in _entries)
                {
                    var entry = zipArchive.CreateEntry($"{name}.npy");
                    using var stream = entry.Open();

                    switch (value)
                    {
                        case float[,] array:
                            WriteNpyArray(stream, array);
                            break;
                        case string str:
                            WriteNpyString(stream, str);
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported data type: {value.GetType()}");
                    }
                }
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            throw;
        }
    }

    /// <summary>
    /// Convenience method to write a TrainingDataSample to an NPZ file.
    /// </summary>
    /// <param name="path">Output file path.</param>
    /// <param name="sample">Training data sample to write.</param>
    public static void WriteSample(string path, TrainingDataSample sample)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(sample);

        var writer = new NpzWriter();
        writer.AddArray("mel_spec", sample.MelSpectrogram);
        writer.AddArray("piano_roll", sample.PianoRoll);
        writer.AddArray("onset_roll", sample.OnsetRoll);
        writer.AddArray("offset_roll", sample.OffsetRoll);
        writer.AddArray("velocity_roll", sample.VelocityRoll);

        if (sample.AudioPath != null)
        {
            writer.AddString("audio_path", sample.AudioPath);
        }

        if (sample.MidiPath != null)
        {
            writer.AddString("midi_path", sample.MidiPath);
        }

        writer.Save(path);
    }

    private static void WriteNpyArray(Stream stream, float[,] array)
    {
        // NPY format header
        var rows = array.GetLength(0);
        var cols = array.GetLength(1);

        // Magic number
        stream.WriteByte(0x93);
        stream.Write("NUMPY"u8);

        // Version 1.0
        stream.WriteByte(1);
        stream.WriteByte(0);

        // Header dict
        var headerDict = $"{{'descr': '<f4', 'fortran_order': False, 'shape': ({rows}, {cols}), }}";
        var headerBytes = Encoding.ASCII.GetBytes(headerDict);

        // Pad header to multiple of 64 bytes (including length field)
        var totalHeaderLen = 10 + headerBytes.Length; // 10 = magic(6) + version(2) + len(2)
        var padding = (64 - (totalHeaderLen % 64)) % 64;
        var paddedHeaderLen = headerBytes.Length + padding;

        // Write header length (little-endian ushort)
        stream.WriteByte((byte)(paddedHeaderLen & 0xFF));
        stream.WriteByte((byte)((paddedHeaderLen >> 8) & 0xFF));

        // Write header
        stream.Write(headerBytes);

        // Write padding spaces
        for (int i = 0; i < padding; i++)
        {
            stream.WriteByte(0x20); // space
        }

        // Write data in row-major (C) order
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var bytes = BitConverter.GetBytes(array[i, j]);
                stream.Write(bytes);
            }
        }
    }

    private static void WriteNpyString(Stream stream, string value)
    {
        // NPY format for string (stored as byte array)
        var bytes = Encoding.UTF8.GetBytes(value);

        // Magic number
        stream.WriteByte(0x93);
        stream.Write("NUMPY"u8);

        // Version 1.0
        stream.WriteByte(1);
        stream.WriteByte(0);

        // Header dict
        var headerDict = $"{{'descr': '|S{bytes.Length}', 'fortran_order': False, 'shape': (), }}";
        var headerBytes = Encoding.ASCII.GetBytes(headerDict);

        // Pad header
        var totalHeaderLen = 10 + headerBytes.Length;
        var padding = (64 - (totalHeaderLen % 64)) % 64;
        var paddedHeaderLen = headerBytes.Length + padding;

        // Write header length
        stream.WriteByte((byte)(paddedHeaderLen & 0xFF));
        stream.WriteByte((byte)((paddedHeaderLen >> 8) & 0xFF));

        // Write header and padding
        stream.Write(headerBytes);
        for (int i = 0; i < padding; i++)
        {
            stream.WriteByte(0x20);
        }

        // Write string bytes
        stream.Write(bytes);
    }
}
