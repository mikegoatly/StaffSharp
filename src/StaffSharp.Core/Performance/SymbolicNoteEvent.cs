namespace StaffSharp.Performance;

/// <summary>
/// A note from symbolic sources (MIDI, ABC, MusicXML) that is already in musical time.
/// Unlike QuantizedNoteEvent, this doesn't wrap a raw audio event since it was never audio.
/// </summary>
public sealed record SymbolicNoteEvent : IPerformanceEvent
{
    /// <summary>
    /// Creates a new symbolic note event.
    /// </summary>
    /// <param name="pitch">MIDI note number (supports microtones with fractional values).</param>
    /// <param name="onsetBeats">Musical time onset in beats from the start of the piece.</param>
    /// <param name="durationBeats">Musical duration in beats.</param>
    /// <param name="velocity">Note velocity/loudness (0.0-1.0 normalized).</param>
    /// <param name="voiceHint">Optional suggested voice number for polyphonic music (1-based).</param>
    /// <param name="articulation">Articulation flags from the source format.</param>
    public SymbolicNoteEvent(
        MidiNote pitch,
        Rational onsetBeats,
        Rational durationBeats,
        Velocity velocity,
        int? voiceHint = null,
        ArticulationFlags articulation = ArticulationFlags.None)
    {
        Pitch = pitch;
        OnsetBeats = onsetBeats;
        DurationBeats = durationBeats;
        Velocity = velocity;
        VoiceHint = voiceHint;
        Articulation = articulation;
    }

    /// <summary>
    /// MIDI note number (0-127, supports microtones with fractional values).
    /// </summary>
    public MidiNote Pitch { get; }

    /// <summary>
    /// Musical time onset in beats from the start of the piece.
    /// </summary>
    public Rational OnsetBeats { get; }

    /// <summary>
    /// Musical duration in beats.
    /// </summary>
    public Rational DurationBeats { get; }

    /// <summary>
    /// Note velocity/loudness (0.0-1.0 normalized).
    /// </summary>
    public Velocity Velocity { get; }

    /// <summary>
    /// Suggested voice number for polyphonic music (1-based). Null for monophonic.
    /// </summary>
    public int? VoiceHint { get; }

    /// <summary>
    /// Articulation flags from the source format.
    /// </summary>
    public ArticulationFlags Articulation { get; }

    /// <summary>
    /// The offset time in beats (onset + duration).
    /// </summary>
    public Rational OffsetBeats => OnsetBeats + DurationBeats;
}
