using StaffSharp.Audio.Analysis.Quantization;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that quantizes onsets and pitches to musical time.
/// </summary>
internal sealed class QuantizeStage : PipelineStageBase
{
    private readonly IQuantizer _quantizer;
    protected override string StageName => "Quantize";

    public QuantizeStage(AudioPipelineOptions options, IQuantizer quantizer) : base(options)
    {
        _quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));
    }

    /// <summary>
    /// Quantizes onset times and pitches to note events on a rhythmic grid.
    /// </summary>
    /// <param name="onsets">Array of onset times in seconds.</param>
    /// <param name="pitches">Array of MIDI pitch numbers.</param>
    /// <param name="tempoMap">The tempo map for timing reference.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of quantized note events.</returns>
    public Task<IReadOnlyList<QuantizedNoteEvent>> ExecuteAsync(
        double[] onsets,
        int[] pitches,
        TempoMap tempoMap,
        CancellationToken ct)
    {
        ReportProgress("Quantizing notes");

        ct.ThrowIfCancellationRequested();

        var quantizedNotes = _quantizer.Quantize(onsets, pitches, tempoMap);

        if (quantizedNotes == null)
        {
            throw new InvalidOperationException("Quantization failed - quantizer returned null.");
        }

        EmitDiagnostics("QuantizedNoteCount", quantizedNotes.Count);
        
        // Detailed diagnostics for each quantized note
        if (Options.DiagnosticsCollector != null)
        {
            var startBeats = quantizedNotes.Select(n => n.OnsetBeats.ToDouble()).ToArray();
            var durations = quantizedNotes.Select(n => n.DurationBeats.ToDouble()).ToArray();
            var midiNotes = quantizedNotes.Select(n => n.RawEvent.Pitch.MidiNumber).ToArray();
            var isPitched = quantizedNotes.Select(n => n.RawEvent.Pitch.MidiNumber != DetectPitchesStage.UnpitchedSentinel).ToArray();
            
            EmitDiagnostics("Start beats", startBeats);
            EmitDiagnostics("Durations (beats)", durations);
            EmitDiagnostics("MIDI notes", midiNotes);
            EmitDiagnostics("Is pitched", isPitched);
        }

        return Task.FromResult(quantizedNotes);
    }
}
