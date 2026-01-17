using StaffSharp.Audio.IO;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that loads audio from a WAV stream.
/// </summary>
internal sealed class LoadAudioStage(PipelineProgress progress)
{
    /// <summary>
    /// Loads audio data from a WAV stream.
    /// </summary>
    /// <param name="stream">The input WAV stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded audio buffer.</returns>
    public async Task<AudioBuffer> ExecuteAsync(Stream stream, CancellationToken ct)
    {
        progress.ReportProgress("Loading audio");

        var audio = await WavReader.ReadAsync(stream, ct).ConfigureAwait(false);

        progress.EmitDiagnostics("SampleRate", audio.SampleRate);
        progress.EmitDiagnostics("Channels", audio.Channels);
        progress.EmitDiagnostics("DurationSeconds", audio.DurationSeconds);
        progress.EmitDiagnostics("SampleCount", audio.SampleCount);

        return audio;
    }
}

