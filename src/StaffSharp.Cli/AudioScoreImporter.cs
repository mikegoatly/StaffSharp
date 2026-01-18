using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning;
using StaffSharp.MachineLearning.Options;
using StaffSharp.Notation;

namespace StaffSharp.Cli;

/// <summary>
/// Audio file (WAV) importer that uses the audio-to-score pipeline.
/// </summary>
internal sealed class AudioScoreImporter : IScoreImporter
{
    private string? _modelPath;
    private float? _onsetThreshold;
    private float? _frameThreshold;
    private float? _offsetThreshold;
    private float? _minNoteLength;
    private bool _useMl;

    public string FormatName => "Audio (WAV)";

    public IReadOnlyList<string> SupportedExtensions => [".wav"];

    /// <summary>
    /// Configures ML-based note detection options.
    /// </summary>
    public void ConfigureMLOptions(
        string? modelPath,
        float? onsetThreshold,
        float? frameThreshold,
        float? offsetThreshold,
        float? minNoteLength)
    {
        _useMl = true;
        _modelPath = modelPath;
        _onsetThreshold = onsetThreshold;
        _frameThreshold = frameThreshold;
        _offsetThreshold = offsetThreshold;
        _minNoteLength = minNoteLength;
    }

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

        // Configure ML note detector if requested
        if (_useMl)
        {
            var mlOptions = new MLTranscriptionOptions
            { 
                ModelPath = _modelPath,
            };

            // Don't override the default options unless specified
            if (_onsetThreshold is { } onsetThreshold)
            {
                mlOptions = mlOptions with { OnsetThreshold = onsetThreshold };
            }

            if (_frameThreshold is { } frameThreshold)
            {
                mlOptions = mlOptions with { FrameThreshold = frameThreshold };
            }

            if (_offsetThreshold is { } offsetThreshold)
            {
                mlOptions = mlOptions with { OffsetThreshold = offsetThreshold };
            }

            if (_minNoteLength is { } minNoteLength)
            {
                mlOptions = mlOptions with { MinNoteLengthSeconds = minNoteLength };
            }

            options.NoteDetector = MLNoteDetector.Create(mlOptions);
        }

        return await AudioPipeline.FromWavAsync(stream, options, cancellationToken);
    }
}
