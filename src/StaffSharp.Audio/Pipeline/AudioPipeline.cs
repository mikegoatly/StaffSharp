using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Core.Notation;
using StaffSharp.Notation;

namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Simplified audio-to-score pipeline with implicit execution and automatic parallelization.
/// </summary>
public static class AudioPipeline
{
    /// <summary>
    /// Converts a WAV audio stream to a notation score using default settings.
    /// </summary>
    /// <param name="wavStream">The input WAV stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated notation score.</returns>
    public static Task<NotationScore> FromWavAsync(
        Stream wavStream,
        CancellationToken ct = default)
    {
        return FromWavAsync(wavStream, AudioPipelineOptions.Default, ct);
    }

    /// <summary>
    /// Converts a WAV audio stream to a notation score with custom options.
    /// </summary>
    /// <param name="wavStream">The input WAV stream.</param>
    /// <param name="options">Configuration options for detectors and analyzers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated notation score.</returns>
    public static async Task<NotationScore> FromWavAsync(
        Stream wavStream,
        AudioPipelineOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Execute pipeline with explicit orchestration and parallelization
        var rawAudio = await new LoadAudioStage(options).ExecuteAsync(wavStream, ct).ConfigureAwait(false);
        var audio = await new NormalizeAudioStage(options).ExecuteAsync(rawAudio, ct).ConfigureAwait(false);
        var boundaries = await new DetectBoundariesStage(options, options.BoundaryDetector).ExecuteAsync(audio, ct).ConfigureAwait(false);
        var onsets = await new DetectOnsetsStage(options, options.OnsetDetector).ExecuteAsync(audio, boundaries, ct).ConfigureAwait(false);

        // Pitch and time signature detection run concurrently
        var pitchTask = new DetectPitchesStage(options, options.PitchDetector).ExecuteAsync(onsets, audio, boundaries, ct);
        var tsTask = new DetectTimeSignatureStage(options, options.TimeSignatureDetector).ExecuteAsync(onsets, ct);
        await Task.WhenAll(pitchTask, tsTask).ConfigureAwait(false);
        var pitches = await pitchTask.ConfigureAwait(false);
        var timeSignatures = await tsTask.ConfigureAwait(false);

        var tempoMap = await new DetectTempoStage(options, options.TempoDetector).ExecuteAsync(onsets, timeSignatures, ct).ConfigureAwait(false);
        var quantized = await new QuantizeStage(options, options.Quantizer).ExecuteAsync(onsets, pitches, tempoMap, ct).ConfigureAwait(false);
        var timeline = await new BuildTimelineStage(options).ExecuteAsync(quantized, tempoMap, ct).ConfigureAwait(false);
        var score = await new ConvertToScoreStage(options, new NotationEngine(), new NotationOptions()).ExecuteAsync(timeline, ct).ConfigureAwait(false);

        options.Progress?.Report(new ImportProgress("Import from WAV", "Complete"));

        return score;
    }
}