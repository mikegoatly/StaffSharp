namespace StaffSharp.Quantization;

using StaffSharp.Notation;
using StaffSharp.Performance;

/// <summary>
/// Quantizes polyphonic note events with known durations to musical time.
/// Used by ML-based detectors that produce note events with onset, offset, and pitch.
/// </summary>
public interface IPolyphonicQuantizer
{
    /// <summary>
    /// Quantizes note events to rhythmic grid.
    /// Snaps both onsets and offsets to musical time, preserving polyphony.
    /// </summary>
    /// <param name="notes">Note events with onset, duration, pitch, and velocity.</param>
    /// <param name="timeSignatures">Detected time signatures.</param>
    /// <param name="estimatedTempo">Estimated tempo from onset analysis.</param>
    /// <returns>Quantized note events and refined tempo map.</returns>
    (IReadOnlyList<QuantizedNoteEvent> Notes, TempoMap TempoMap) Quantize(
        IReadOnlyList<NoteEvent> notes,
        IReadOnlyList<TimeSignature> timeSignatures,
        TempoMap estimatedTempo);
}
