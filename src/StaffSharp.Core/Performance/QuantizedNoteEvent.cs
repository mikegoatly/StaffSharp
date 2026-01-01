namespace StaffSharp.Performance;

/// <summary>
/// A note derived from audio analysis, with both real time (from audio) and quantized musical time.
/// Wraps the original NoteEvent to preserve exact audio timing for re-quantization experiments.
/// This is the "wrapper pattern" from the architecture - preserves original data while adding musical time.
/// </summary>
public sealed record QuantizedNoteEvent : INoteEvent
{
    /// <summary>
    /// Creates a new quantized note event.
    /// </summary>
    /// <param name="rawEvent">The original audio-derived note event with exact timing.</param>
    /// <param name="onsetBeats">The quantized musical time onset in beats.</param>
    /// <param name="durationBeats">The quantized musical duration in beats.</param>
    /// <param name="quantizationMetadata">Metadata about the quantization process.</param>
    /// <param name="voiceHint">Optional suggested voice number for polyphonic music (1-based, null for monophonic).</param>
    /// <param name="articulation">Articulation flags detected from audio analysis.</param>
    public QuantizedNoteEvent(
        NoteEvent rawEvent,
        Rational onsetBeats,
        Rational durationBeats,
        QuantizationMetadata quantizationMetadata,
        int? voiceHint = null,
        ArticulationFlags articulation = ArticulationFlags.None)
    {
        RawEvent = rawEvent;
        OnsetBeats = onsetBeats;
        DurationBeats = durationBeats;
        QuantizationMetadata = quantizationMetadata;
        VoiceHint = voiceHint;
        Articulation = articulation;
    }

    /// <summary>
    /// The original audio-derived note event, preserving exact timing from audio analysis.
    /// Allows re-quantization with different parameters without data loss.
    /// </summary>
    public NoteEvent RawEvent { get; }

    /// <summary>
    /// Musical time onset in beats from the start of the piece.
    /// </summary>
    public Rational OnsetBeats { get; }

    /// <summary>
    /// Musical duration in beats.
    /// </summary>
    public Rational DurationBeats { get; }

    /// <summary>
    /// Metadata about the quantization process (errors, subdivision, tempo).
    /// </summary>
    public QuantizationMetadata QuantizationMetadata { get; }

    /// <summary>
    /// Suggested voice number for polyphonic music (1-based). Null for monophonic.
    /// This is a hint for the NotationEngine; it may reassign voices for better engraving.
    /// </summary>
    public int? VoiceHint { get; }

    /// <summary>
    /// Articulation detected from audio analysis (staccato, accent, etc.).
    /// </summary>
    public ArticulationFlags Articulation { get; }

    /// <summary>
    /// MIDI note number representing the pitch (delegates to RawEvent.Pitch).
    /// </summary>
    public MidiNote Pitch => RawEvent.Pitch;

    /// <summary>
    /// Note velocity/loudness (delegates to RawEvent.Velocity).
    /// </summary>
    public Velocity Velocity => RawEvent.Velocity;

    /// <summary>
    /// The offset time in beats (onset + duration).
    /// </summary>
    public Rational OffsetBeats => OnsetBeats + DurationBeats;
}
