using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Analysis.Quantization;
using StaffSharp;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that quantizes onsets and pitches to musical time.
/// </summary>
internal sealed class QuantizeStage : IAsyncPipelineStage<TempoMap, IReadOnlyList<QuantizedNoteEvent>>
{
    private readonly IQuantizer _quantizer;

    public string StageName => "Quantize";

    public QuantizeStage(IQuantizer quantizer)
    {
        _quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));
    }

    public Task<IReadOnlyList<QuantizedNoteEvent>> ProcessAsync(TempoMap input, AudioPipelineContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.Onsets is not { } onsets)
        {
            throw new InvalidOperationException("Onsets not available in context.");
        }

        if (context.Pitches is not { } pitches)
        {
            throw new InvalidOperationException("Pitches not available in context.");
        }

        var quantizedNotes = _quantizer.Quantize(onsets.Span, pitches.Span, input);

        if (quantizedNotes == null)
        {
            throw new InvalidOperationException("Quantization failed - quantizer returned null.");
        }

        context.EmitDiagnostics(StageName, "QuantizedNoteCount", quantizedNotes.Count);
        context.EmitDiagnostics(StageName, "QuantizedNotes", () => quantizedNotes);

        context.QuantizedNotes = quantizedNotes;
        return Task.FromResult(quantizedNotes);
    }
}
