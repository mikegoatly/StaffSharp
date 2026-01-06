using StaffSharp.Audio.IO;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that loads audio from a WAV stream.
/// </summary>
internal sealed class LoadAudioStage : PipelineStageBase
{
    protected override string StageName => "LoadAudio";

    public LoadAudioStage(AudioPipelineOptions options) : base(options)
    {
    }

    /// <summary>
    /// Loads audio data from a WAV stream.
    /// </summary>
    /// <param name="stream">The input WAV stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded audio buffer.</returns>
    public async Task<AudioBuffer> ExecuteAsync(Stream stream, CancellationToken ct)
    {
        ReportProgress("Loading audio");

        var audio = await WavReader.ReadAsync(stream, ct).ConfigureAwait(false);

        EmitDiagnostics("SampleRate", audio.SampleRate);
        EmitDiagnostics("Channels", audio.Channels);
        EmitDiagnostics("DurationSeconds", audio.DurationSeconds);
        EmitDiagnostics("SampleCount", audio.SampleCount);

        return audio;
    }
}

