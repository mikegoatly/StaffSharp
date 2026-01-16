namespace StaffSharp.Quantization;

using StaffSharp.Performance;

/// <summary>
/// Result of note detection containing quantized notes and tempo information.
/// </summary>
/// <param name="Notes">Quantized note events aligned to musical time.</param>
/// <param name="TempoMap">Tempo and time signature information for the transcription.</param>
public readonly record struct QuantizationResult(
    IReadOnlyList<QuantizedNoteEvent> Notes,
    TempoMap TempoMap
);
