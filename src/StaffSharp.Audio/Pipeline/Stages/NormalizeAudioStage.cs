using StaffSharp.Audio;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that normalizes audio (mixes to mono and normalizes volume).
/// </summary>
internal sealed class NormalizeAudioStage : PipelineStageBase
{
    protected override string StageName => "NormalizeAudio";

    public NormalizeAudioStage(AudioPipelineOptions options) : base(options)
    {
    }

    /// <summary>
    /// Normalizes the audio buffer.
    /// </summary>
    /// <param name="audio">The input audio buffer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The normalized audio buffer.</returns>
    public Task<AudioBuffer> ExecuteAsync(AudioBuffer audio, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ReportProgress("Normalizing audio");

        // Convert to mono first
        var monoAudio = audio.ToMono();
        
        // Then normalize volume
        var (normalized, normalizationStats) = monoAudio.Normalize();

        EmitDiagnostics("Channels", normalized.Channels);
        EmitDiagnostics("SampleCount", normalized.SampleCount);
        EmitDiagnostics("NormalizationStats", normalizationStats);

        return Task.FromResult(normalized);
    }
}
