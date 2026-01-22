namespace StaffSharp.Quantization;

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
    /// <param name="tempoMap">Tempo map containing tempo changes and time signatures.</param>
    /// <returns>Quantized note events and refined tempo map.</returns>
    (IReadOnlyList<QuantizedNoteEvent> Notes, TempoMap TempoMap) Quantize(
        IReadOnlyList<NoteEvent> notes,
        TempoMap tempoMap);
}
