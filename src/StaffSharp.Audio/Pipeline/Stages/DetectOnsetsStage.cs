using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Boundaries;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects note onset times.
/// </summary>
internal sealed class DetectOnsetsStage : IAsyncPipelineStage<AudioBoundaries, double[]>
{
    private readonly IOnsetDetector _detector;

    public string StageName => "DetectOnsets";

    public DetectOnsetsStage(IOnsetDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    public Task<double[]> ProcessAsync(AudioBoundaries input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.Audio == null)
        {
            throw new InvalidOperationException("Audio buffer not available in context. Ensure LoadAudioStage has executed.");
        }

        // Slice the audio to the content region
        var slice = context.Audio.Samples.Span.Slice(
            input.StartSample,
            input.EndSample - input.StartSample
        );

        var onsets = _detector.DetectOnsets(
            slice,
            input.SampleRate,
            startTimeOffset: input.LeadingSilence.TotalSeconds
        );

        context.EmitDiagnostics(StageName, "OnsetCount", onsets.Length);
        context.EmitDiagnostics(StageName, "Onsets", () => onsets);

        context.Onsets = onsets;
        return Task.FromResult(onsets);
    }
}
