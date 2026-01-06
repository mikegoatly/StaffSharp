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

        // Create stages with injected dependencies - each stage handles its own progress/diagnostics
        var loadAudio = new LoadAudioStage(options);
        var detectBoundaries = new DetectBoundariesStage(options, options.BoundaryDetector);
        var detectOnsets = new DetectOnsetsStage(options, options.OnsetDetector);
        var detectPitches = new DetectPitchesStage(options, options.PitchDetector);
        var detectTimeSignature = new DetectTimeSignatureStage(options, options.TimeSignatureDetector);
        var detectTempo = new DetectTempoStage(options, options.TempoDetector);
        var quantize = new QuantizeStage(options, options.Quantizer);
        var buildTimeline = new BuildTimelineStage(options);
        var convertToScore = new ConvertToScoreStage(options, new NotationEngine(), new NotationOptions());

        // Execute pipeline with explicit orchestration and parallelization
        var audio = await loadAudio.ExecuteAsync(wavStream, ct).ConfigureAwait(false);
        var boundaries = await detectBoundaries.ExecuteAsync(audio, ct).ConfigureAwait(false);
        var onsets = await detectOnsets.ExecuteAsync(audio, boundaries, ct).ConfigureAwait(false);

        // PARALLEL EXECUTION: Pitch and time signature detection run concurrently
        var pitchTask = detectPitches.ExecuteAsync(onsets, audio, boundaries, ct);
        var tsTask = detectTimeSignature.ExecuteAsync(onsets, ct);
        await Task.WhenAll(pitchTask, tsTask).ConfigureAwait(false);
        var pitches = pitchTask.GetAwaiter().GetResult();
        var timeSignatures = tsTask.GetAwaiter().GetResult();

        var tempoMap = await detectTempo.ExecuteAsync(onsets, timeSignatures, ct).ConfigureAwait(false);
        var quantized = await quantize.ExecuteAsync(onsets, pitches, tempoMap, ct).ConfigureAwait(false);
        var timeline = await buildTimeline.ExecuteAsync(quantized, tempoMap, ct).ConfigureAwait(false);
        var score = await convertToScore.ExecuteAsync(timeline, ct).ConfigureAwait(false);

        options.Progress?.Report(new ImportProgress("Import from WAV", "Complete"));

        return score;
    }
}