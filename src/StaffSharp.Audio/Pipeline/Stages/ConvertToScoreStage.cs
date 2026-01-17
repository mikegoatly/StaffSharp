using StaffSharp.Core.Notation;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that converts a PerformanceTimeline (IR1) to a NotationScore (IR2).
/// </summary>
internal sealed class ConvertToScoreStage
{
    private readonly INotationEngine _engine;
    private readonly NotationOptions _notationOptions;

    public ConvertToScoreStage(INotationEngine engine, NotationOptions notationOptions)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notationOptions = notationOptions ?? throw new ArgumentNullException(nameof(notationOptions));
    }

    /// <summary>
    /// Converts a performance timeline to a notation score (IR1 → IR2).
    /// </summary>
    /// <param name="timeline">The performance timeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The notation score.</returns>
    public Task<NotationScore> ExecuteAsync(PipelineProgress progress, PerformanceTimeline timeline, CancellationToken ct)
    {
        progress.ReportProgress("Converting to score");

        ct.ThrowIfCancellationRequested();

        var score = _engine.Convert(timeline, _notationOptions);

        // Detailed measure diagnostics
        if (progress.DiagnosticsEnabled)
        {
            progress.EmitDiagnostics("PartCount", score.Parts.Count);
            if (score.Parts.Count > 0)
            {
                var totalVoices = score.Parts.Sum(p => p.Voices.Count);
                progress.EmitDiagnostics("TotalVoices", totalVoices);


                foreach (var (part, partIndex) in score.Parts.Select((p, i) => (p, i)))
                {
                    foreach (var (voice, voiceIndex) in part.Voices.Select((v, i) => (v, i)))
                    {
                        progress.EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1} - Measure count", voice.Measures.Count);

                        foreach (var (measure, measureIndex) in voice.Measures.Select((m, i) => (m, i)))
                        {
                            var eventCount = measure.Events.Count;
                            var totalDuration = measure.Events.Sum(e => e.Duration.ToBeats().ToDouble());
                            var eventTypes = string.Join(", ", measure.Events.Select(e =>
                                e is NotationNote ? "Note" : e is Rest ? "Rest" : e.GetType().Name));
                            var durations = string.Join(", ", measure.Events.Select(e => e.Duration.ToString()));

                            progress.EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Events", eventCount);
                            progress.EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Total duration", totalDuration);
                            progress.EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Event types", eventTypes);
                            progress.EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Durations", durations);
                        }
                    }
                }
            }
        }

        return Task.FromResult(score);
    }
}
