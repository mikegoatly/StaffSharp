using StaffSharp.Core.Notation;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that converts a PerformanceTimeline (IR1) to a NotationScore (IR2).
/// </summary>
internal sealed class ConvertToScoreStage : PipelineStageBase
{
    private readonly INotationEngine _engine;
    private readonly NotationOptions _notationOptions;
    protected override string StageName => "ConvertToScore";

    public ConvertToScoreStage(AudioPipelineOptions options, INotationEngine engine, NotationOptions notationOptions) : base(options)
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
    public Task<NotationScore> ExecuteAsync(PerformanceTimeline timeline, CancellationToken ct)
    {
        ReportProgress("Converting to score");

        ct.ThrowIfCancellationRequested();

        var score = _engine.Convert(timeline, _notationOptions);

        EmitDiagnostics("PartCount", score.Parts.Count);
        if (score.Parts.Count > 0)
        {
            var totalVoices = score.Parts.Sum(p => p.Voices.Count);
            EmitDiagnostics("TotalVoices", totalVoices);
            
            // Detailed measure diagnostics
            if (Options.DiagnosticsCollector != null)
            {
                foreach (var (part, partIndex) in score.Parts.Select((p, i) => (p, i)))
                {
                    foreach (var (voice, voiceIndex) in part.Voices.Select((v, i) => (v, i)))
                    {
                        EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1} - Measure count", voice.Measures.Count);
                        
                        foreach (var (measure, measureIndex) in voice.Measures.Select((m, i) => (m, i)))
                        {
                            var eventCount = measure.Events.Count;
                            var totalDuration = measure.Events.Sum(e => e.Duration.ToBeats().ToDouble());
                            var eventTypes = string.Join(", ", measure.Events.Select(e => 
                                e is NotationNote ? "Note" : e is Rest ? "Rest" : e.GetType().Name));
                            var durations = string.Join(", ", measure.Events.Select(e => e.Duration.ToString()));
                            
                            EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Events", eventCount);
                            EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Total duration", totalDuration);
                            EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Event types", eventTypes);
                            EmitDiagnostics($"Part {partIndex + 1}, Voice {voiceIndex + 1}, Measure {measureIndex + 1} - Durations", durations);
                        }
                    }
                }
            }
        }

        return Task.FromResult(score);
    }
}
