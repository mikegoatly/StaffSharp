using StaffSharp.Audio.Pipeline;
using StaffSharp.Notation;

namespace StaffSharp.Cli;

/// <summary>
/// Audio file (WAV) importer that uses the audio-to-score pipeline.
/// </summary>
internal sealed class AudioScoreImporter : IScoreImporter
{
    public string FormatName => "Audio (WAV)";

    public IReadOnlyList<string> SupportedExtensions => [".wav"];

    public async Task<NotationScore> ImportAsync(
        Stream stream,
        IProgress<ImportProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var options = new AudioPipelineOptions
        {
            Progress = progress,
            DiagnosticsCollector = progress is not null ? new CliDiagnosticsCollector() : null,
        };

        return await AudioPipeline.FromWavAsync(stream, options, cancellationToken);
    }
}
