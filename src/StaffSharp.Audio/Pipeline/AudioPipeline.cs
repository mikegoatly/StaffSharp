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

        var rawAudio = await new LoadAudioStage(options).ExecuteAsync(wavStream, ct).ConfigureAwait(false);
        
        return await FromAudioBufferAsync(rawAudio, options, ct).ConfigureAwait(false);
    }

    public static async Task<NotationScore> FromAudioBufferAsync(
        AudioBuffer audioBuffer,
        AudioPipelineOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(audioBuffer);
        ArgumentNullException.ThrowIfNull(options);

        // Step 1: Normalize audio
        var audio = await new NormalizeAudioStage(options).ExecuteAsync(audioBuffer, ct).ConfigureAwait(false);

        // Step 2: Note detection (sub-pipeline: detect → tempo → quantize)
        var quantizationResult = await options.NoteDetector.DetectAsync(audio, ct).ConfigureAwait(false);

        // Step 3: Build timeline from quantized notes
        var timeline = await new BuildTimelineStage(options)
            .ExecuteAsync(quantizationResult.Notes, quantizationResult.TempoMap, ct).ConfigureAwait(false);

        // Step 4: Convert to notation score
        var score = await new ConvertToScoreStage(options, new NotationEngine(), new NotationOptions())
            .ExecuteAsync(timeline, ct).ConfigureAwait(false);

        options.Progress?.Report(new("Import", "Complete"));

        return score;
    }
}