using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Analysis.Boundaries;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects audio boundaries (leading/trailing silence).
/// </summary>
internal sealed class DetectBoundariesStage : IAsyncPipelineStage<AudioBuffer, AudioBoundaries>
{
    private readonly IAudioBoundaryDetector _detector;

    public string StageName => "DetectBoundaries";

    public DetectBoundariesStage(IAudioBoundaryDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    public Task<AudioBoundaries> ProcessAsync(AudioBuffer input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var boundaries = _detector.DetectBoundaries(input);

        if (boundaries == null)
        {
            throw new InvalidOperationException("Boundary detection failed - detector returned null.");
        }

        context.EmitDiagnostics(StageName, "StartSample", boundaries.StartSample);
        context.EmitDiagnostics(StageName, "EndSample", boundaries.EndSample);
        context.EmitDiagnostics(StageName, "LeadingSilence", boundaries.LeadingSilence);
        context.EmitDiagnostics(StageName, "TrailingSilence", boundaries.TrailingSilence);
        context.EmitDiagnostics(StageName, "ContentDuration", boundaries.ContentDuration);

        context.Boundaries = boundaries;
        return Task.FromResult(boundaries);
    }
}
