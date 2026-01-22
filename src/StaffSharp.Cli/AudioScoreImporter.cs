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
    private MLTranscriptionOptions? _mlOptions;

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
        float? minNoteLength,
        float? minGapSeconds,
        float? minVelocity,
        float? minFrameForOnset)
    {
        _mlOptions = new MLTranscriptionOptions
        {
            ModelPath = modelPath,
        };

        // Don't override the default options unless specified
        if (onsetThreshold is not null)
        {
            _mlOptions = _mlOptions with { OnsetThreshold = onsetThreshold.GetValueOrDefault() };
        }

        if (frameThreshold is not null)
        {
            _mlOptions = _mlOptions with { FrameThreshold = frameThreshold.GetValueOrDefault() };
        }

        if (offsetThreshold is not null)
        {
            _mlOptions = _mlOptions with { OffsetThreshold = offsetThreshold.GetValueOrDefault() };
        }

        if (minNoteLength is not null)
        {
            _mlOptions = _mlOptions with { MinNoteLengthSeconds = minNoteLength.GetValueOrDefault() };
        }

        if (minGapSeconds is not null)
        {
            _mlOptions = _mlOptions with { MinGapSeconds = minGapSeconds.GetValueOrDefault() };
        }

        if (minVelocity is not null)
        {
            _mlOptions = _mlOptions with { MinVelocity = minVelocity.GetValueOrDefault() };
        }

        if (minFrameForOnset is not null)
        {
            _mlOptions = _mlOptions with { MinFrameForOnset = minFrameForOnset.GetValueOrDefault() };
        }
    }

    public async Task<NotationScore> ImportAsync(
        Stream stream,
        IProgress<ImportProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var options = new AudioPipelineOptions
        {
            Progress = progress,
            DiagnosticsCollector = progress is not null ? CliDiagnosticsCollector.Instance : null,
        };

        // Configure ML note detector if requested
        if (_mlOptions is not null)
        {
            options.NoteDetector = MLNoteDetector.Create(_mlOptions);
        }

        return await AudioPipeline.FromWavAsync(stream, options, cancellationToken);
    }
}
