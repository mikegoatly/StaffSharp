using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Quantization;

/// <summary>
/// Interface for rhythm quantization algorithms.
/// Converts onset times and pitches to quantized note events with rational durations.
/// </summary>
public interface IQuantizer
{
    /// <summary>
    /// Quantizes onset times and pitches to note events.
    /// </summary>
    /// <param name="onsetTimes">Array of onset times in seconds.</param>
    /// <param name="pitches">Array of MIDI pitch numbers corresponding to each onset.</param>
    /// <param name="tempoMap">Tempo and time signature information.</param>
    /// <returns>List of quantized note events, or null if quantization fails.</returns>
    IReadOnlyList<QuantizedNoteEvent>? Quantize(
        ReadOnlySpan<double> onsetTimes,
        ReadOnlySpan<int> pitches,
        TempoMap tempoMap);
}
