using StaffSharp.Notation;
using StaffSharp.Performance;
using StaffSharp.Core.Notation;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that converts a PerformanceTimeline (IR1) to a NotationScore (IR2).
/// </summary>
internal sealed class ConvertToScoreStage : IAsyncPipelineStage<PerformanceTimeline, NotationScore>
{
    private readonly INotationEngine _engine;
    private readonly NotationOptions _options;

    public string StageName => "ConvertToScore";

    public ConvertToScoreStage(INotationEngine engine, NotationOptions options)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<NotationScore> ProcessAsync(PerformanceTimeline input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var score = _engine.Convert(input, _options);

        context.EmitDiagnostics(StageName, "PartCount", score.Parts.Count);
        context.EmitDiagnostics(StageName, "Title", score.Metadata.Title ?? "(none)");
        context.EmitDiagnostics(StageName, "KeySignature", score.Metadata.KeySignature.ToString());

        if (score.Parts.Count > 0)
        {
            var totalVoices = score.Parts.Sum(p => p.Voices.Count);
            context.EmitDiagnostics(StageName, "TotalVoices", totalVoices);
        }

        return Task.FromResult(score);
    }
}
