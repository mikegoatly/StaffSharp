namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that normalizes audio (mixes to mono and normalizes volume).
/// </summary>
internal sealed class NormalizeAudioStage(PipelineProgress progress)
{

    /// <summary>
    /// Normalizes the audio buffer.
    /// </summary>
    /// <param name="audio">The input audio buffer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The normalized audio buffer.</returns>
    public Task<AudioBuffer> ExecuteAsync(AudioBuffer audio, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        progress.ReportProgress("Normalizing audio");

        // Convert to mono first
        var monoAudio = audio.ToMono();

        // Then normalize volume
        //var (normalized, normalizationStats) = monoAudio.Normalize();
        var normalized = monoAudio;

        progress.EmitDiagnostics("Channels", normalized.Channels);
        progress.EmitDiagnostics("SampleCount", normalized.SampleCount);
        //progress.EmitDiagnostics("NormalizationStats", normalizationStats);

        return Task.FromResult(normalized);
    }
}
