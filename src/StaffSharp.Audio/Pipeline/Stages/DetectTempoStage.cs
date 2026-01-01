using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects tempo and builds a TempoMap.
/// </summary>
internal sealed class DetectTempoStage : IAsyncPipelineStage<IReadOnlyList<TimeSignatureChange>, TempoMap>
{
    private readonly ITempoDetector _detector;

    public string StageName => "DetectTempo";

    public DetectTempoStage(ITempoDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    public Task<TempoMap> ProcessAsync(IReadOnlyList<TimeSignatureChange> input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.Onsets is not { } onsets)
        {
            throw new InvalidOperationException("Onsets not available in context.");
        }

        var tempoMap = _detector.DetectTempo(onsets.Span);

        if (tempoMap is null)
        {
            throw new InvalidOperationException("Tempo detection failed - detector returned null.");
        }

        context.EmitDiagnostics(StageName, "TempoChangeCount", tempoMap.TempoChanges.Count);
        context.EmitDiagnostics(StageName, "TimeSignatureCount", tempoMap.TimeSignatures.Count);

        if (tempoMap.TempoChanges.Count > 0)
        {
            context.EmitDiagnostics(StageName, "InitialTempo", tempoMap.TempoChanges[0].BeatsPerMinute);
        }

        context.TempoMap = tempoMap;
        return Task.FromResult(tempoMap);
    }
}
