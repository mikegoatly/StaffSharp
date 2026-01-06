using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that detects tempo and builds a TempoMap.
/// </summary>
internal sealed class DetectTempoStage : PipelineStageBase
{
    private readonly ITempoDetector _detector;
    protected override string StageName => "DetectTempo";

    public DetectTempoStage(AudioPipelineOptions options, ITempoDetector detector) : base(options)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    /// <summary>
    /// Detects tempo from onset timing patterns and builds a tempo map.
    /// </summary>
    /// <param name="onsets">Array of onset times in seconds.</param>
    /// <param name="timeSignatures">Detected time signatures.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tempo map with tempo changes and time signatures.</returns>
    public Task<TempoMap> ExecuteAsync(
        double[] onsets,
        IReadOnlyList<TimeSignatureChange> timeSignatures,
        CancellationToken ct)
    {
        ReportProgress("Detecting tempo");

        ct.ThrowIfCancellationRequested();

        var tempoMap = _detector.DetectTempo(onsets);

        if (tempoMap is null)
        {
            throw new InvalidOperationException("Tempo detection failed - detector returned null.");
        }

        if (tempoMap.TempoChanges.Count > 0)
        {
            EmitDiagnostics("InitialTempo", tempoMap.TempoChanges[0].BeatsPerMinute);
        }

        EmitDiagnostics("TempoChangeCount", tempoMap.TempoChanges.Count);

        return Task.FromResult(tempoMap);
    }
}
