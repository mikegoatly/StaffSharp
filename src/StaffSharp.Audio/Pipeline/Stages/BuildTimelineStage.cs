using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that builds a PerformanceTimeline from quantized notes.
/// </summary>
internal sealed class BuildTimelineStage : IAsyncPipelineStage<IReadOnlyList<QuantizedNoteEvent>, PerformanceTimeline>
{
    public string StageName => "BuildTimeline";

    public Task<PerformanceTimeline> ProcessAsync(IReadOnlyList<QuantizedNoteEvent> input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.TempoMap == null)
        {
            throw new InvalidOperationException("TempoMap not available in context.");
        }

        var metadata = new PerformanceMetadata(
            Title: null,
            Composer: null,
            Copyright: null
        );

        var timeline = new PerformanceTimeline(
            context.TempoMap,
            input,
            metadata
        );

        context.EmitDiagnostics(StageName, "EventCount", timeline.Events.Count);
        context.EmitDiagnostics(StageName, "TotalDurationBeats", () => timeline.TotalDurationBeats);

        context.Timeline = timeline;
        return Task.FromResult(timeline);
    }
}
