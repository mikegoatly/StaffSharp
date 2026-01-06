using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that builds a PerformanceTimeline from quantized notes.
/// </summary>
internal sealed class BuildTimelineStage : PipelineStageBase
{
    protected override string StageName => "BuildTimeline";

    public BuildTimelineStage(AudioPipelineOptions options) : base(options)
    {
    }

    /// <summary>
    /// Builds a performance timeline (IR1) from quantized note events.
    /// </summary>
    /// <param name="quantizedNotes">The quantized note events.</param>
    /// <param name="tempoMap">The tempo map.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The performance timeline.</returns>
    public Task<PerformanceTimeline> ExecuteAsync(
        IReadOnlyList<QuantizedNoteEvent> quantizedNotes,
        TempoMap tempoMap,
        CancellationToken ct)
    {
        ReportProgress("Building timeline");

        ct.ThrowIfCancellationRequested();

        var metadata = new PerformanceMetadata(
            Title: null,
            Composer: null,
            Copyright: null);

        var timeline = new PerformanceTimeline(
            tempoMap,
            quantizedNotes,
            metadata);

        EmitDiagnostics("EventCount", timeline.Events.Count);
        EmitDiagnostics("TotalDurationBeats", timeline.TotalDurationBeats);

        return Task.FromResult(timeline);
    }
}

