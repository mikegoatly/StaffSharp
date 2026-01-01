using StaffSharp.Audio.IO;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that loads audio from a stream.
/// </summary>
internal sealed class LoadAudioStage : IAsyncPipelineStage<Stream, AudioBuffer>
{
    public string StageName => "LoadAudio";

    public async Task<AudioBuffer> ProcessAsync(Stream input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var audio = await WavReader.ReadAsync(input, context.CancellationToken).ConfigureAwait(false);

        context.EmitDiagnostics(StageName, "SampleRate", audio.SampleRate);
        context.EmitDiagnostics(StageName, "Channels", audio.Channels);
        context.EmitDiagnostics(StageName, "DurationSeconds", audio.DurationSeconds);
        context.EmitDiagnostics(StageName, "SampleCount", audio.SampleCount);

        context.Audio = audio;
        return audio;
    }
}
