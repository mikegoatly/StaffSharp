namespace StaffSharp.Performance;

/// <summary>
/// Metadata about quantization decisions for an audio-derived note.
/// Helps with debugging and allows re-quantization with different parameters.
/// </summary>
/// <param name="Subdivision">Quantization subdivision (4=quarter, 8=eighth, 16=sixteenth, 32=thirty-second).</param>
/// <param name="TempoAtOnset">The tempo (BPM) when the note started, used for time→beat conversion.</param>
/// <param name="OnsetError">How much the onset time was shifted during quantization.</param>
/// <param name="DurationError">How much the duration was adjusted during quantization.</param>
public sealed record QuantizationMetadata(
    int Subdivision,
    double TempoAtOnset,
    TimeSpan OnsetError,
    TimeSpan DurationError);
