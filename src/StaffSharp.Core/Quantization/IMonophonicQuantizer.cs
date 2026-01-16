namespace StaffSharp.Quantization;

using StaffSharp.Notation;
using StaffSharp.Performance;

/// <summary>
/// Quantizes monophonic note data (onset/pitch pairs) to musical time.
/// Used by algorithmic detectors that produce onset times and pitch per onset.
/// </summary>
public interface IMonophonicQuantizer
{
    /// <summary>
    /// Quantizes onset times and pitches to musical note events.
    /// Infers durations from onset spacing and snaps to rhythmic grid.
    /// </summary>
    /// <param name="onsets">Onset times in seconds.</param>
    /// <param name="pitches">MIDI pitch numbers (one per onset).</param>
    /// <param name="timeSignatures">Detected time signatures.</param>
    /// <param name="estimatedTempo">Estimated tempo from onset analysis.</param>
    /// <returns">Quantized note events and refined tempo map.</returns>
    (IReadOnlyList<QuantizedNoteEvent> Notes, TempoMap TempoMap) Quantize(
        ReadOnlySpan<double> onsets,
        ReadOnlySpan<int> pitches,
        IReadOnlyList<TimeSignature> timeSignatures,
        TempoMap estimatedTempo);
}
