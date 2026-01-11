using StaffSharp.Audio;
using StaffSharp.Audio.IO;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Notation;

namespace StaffSharp.Demo.Services;

/// <summary>
/// Audio file (WAV) importer that uses the audio-to-score pipeline.
/// </summary>
internal sealed class AudioScoreImporter() : IScoreImporter
{
    public string FormatName => "Audio (WAV)";

    public IReadOnlyList<string> SupportedExtensions => [".wav"];

    public AudioBuffer? LastAudioBuffer { get; private set; }

    public AudioPipelineOptions Options { get; set; } = new AudioPipelineOptions();

    public async Task<NotationScore> ImportAsync(
        Stream stream,
        IProgress<ImportProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var audioBuffer = await WavReader.ReadAsync(stream, cancellationToken);

        var result = await AudioPipeline.FromAudioBufferAsync(
            audioBuffer,
            Options with { Progress = progress },
            cancellationToken);

        LastAudioBuffer = audioBuffer;

        return result;
    }
}
