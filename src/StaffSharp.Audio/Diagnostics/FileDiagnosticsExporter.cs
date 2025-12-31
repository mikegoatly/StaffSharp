using System.Text.Json;

namespace StaffSharp.Audio.Diagnostics;

/// <summary>
/// Exports diagnostic data to files for inspection and visualization.
/// Writes arrays as CSV for easy plotting, complex objects as JSON.
/// </summary>
public sealed class FileDiagnosticsExporter
{
    private readonly string _outputDirectory;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    /// <summary>
    /// Creates a new file diagnostics exporter.
    /// </summary>
    /// <param name="outputDirectory">Directory to write diagnostic files to.</param>
    public FileDiagnosticsExporter(string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(outputDirectory);
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(outputDirectory);
    }

    /// <summary>
    /// Exports all diagnostics from a collector to files.
    /// </summary>
    /// <param name="collector">The diagnostics collector to export from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExportAsync(
        IDiagnosticsCollector collector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collector);

        var diagnostics = collector.GetDiagnostics();

        foreach (var (stageName, data) in diagnostics)
        {
            var stageDir = Path.Combine(_outputDirectory, SanitizeFileName(stageName));
            Directory.CreateDirectory(stageDir);

            foreach (var (key, value) in data)
            {
                var baseFileName = SanitizeFileName(key);
                var filePath = Path.Combine(stageDir, baseFileName);

                // Route to appropriate exporter based on type
                await ExportValueAsync(filePath, value, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task ExportValueAsync(
        string basePath,
        object value,
        CancellationToken cancellationToken)
    {
        switch (value)
        {
            // Float arrays - write as CSV for plotting
            case float[] floatArray:
                await WriteFloatArrayAsync(basePath + ".csv", floatArray, cancellationToken).ConfigureAwait(false);
                break;

            // Double arrays - write as CSV
            case double[] doubleArray:
                await WriteDoubleArrayAsync(basePath + ".csv", doubleArray, cancellationToken).ConfigureAwait(false);
                break;

            // TimeSpan lists - write as CSV (seconds)
            case List<TimeSpan> timeSpans:
                await WriteTimestampsAsync(basePath + ".csv", timeSpans, cancellationToken).ConfigureAwait(false);
                break;

            case TimeSpan[] timeSpanArray:
                await WriteTimestampsAsync(basePath + ".csv", timeSpanArray, cancellationToken).ConfigureAwait(false);
                break;

            // Complex objects - write as JSON
            default:
                await WriteJsonAsync(basePath + ".json", value, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private static async Task WriteFloatArrayAsync(
        string path,
        float[] data,
        CancellationToken cancellationToken)
    {
        using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("Index,Value").ConfigureAwait(false);

        for (int i = 0; i < data.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await writer.WriteLineAsync($"{i},{data[i]}").ConfigureAwait(false);
        }
    }

    private static async Task WriteDoubleArrayAsync(
        string path,
        double[] data,
        CancellationToken cancellationToken)
    {
        using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("Index,Value").ConfigureAwait(false);

        for (int i = 0; i < data.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await writer.WriteLineAsync($"{i},{data[i]}").ConfigureAwait(false);
        }
    }

    private static async Task WriteTimestampsAsync(
        string path,
        IEnumerable<TimeSpan> timestamps,
        CancellationToken cancellationToken)
    {
        using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("Index,Seconds").ConfigureAwait(false);

        int index = 0;
        foreach (var timestamp in timestamps)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await writer.WriteLineAsync($"{index},{timestamp.TotalSeconds}").ConfigureAwait(false);
            index++;
        }
    }

    private static async Task WriteJsonAsync(
        string path,
        object value,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, value.GetType(), s_jsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove or replace invalid file name characters
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
