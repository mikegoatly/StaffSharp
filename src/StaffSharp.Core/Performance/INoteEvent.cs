namespace StaffSharp.Performance;

/// <summary>
/// Interface for performance events that represent musical notes.
/// Provides common properties for pitch, duration, and velocity regardless of source (audio or symbolic).
/// </summary>
public interface INoteEvent : IPerformanceEvent
{
    /// <summary>
    /// Musical duration in beats.
    /// </summary>
    Rational DurationBeats { get; }

    /// <summary>
    /// MIDI note number representing the pitch.
    /// </summary>
    MidiNote Pitch { get; }

    /// <summary>
    /// Note velocity/loudness (0.0-1.0 normalized).
    /// </summary>
    Velocity Velocity { get; }

    /// <summary>
    /// Articulation flags (staccato, accent, etc.).
    /// </summary>
    ArticulationFlags Articulation { get; }

    /// <summary>
    /// Suggested voice number for polyphonic music (1-based). Null for monophonic.
    /// </summary>
    int? VoiceHint { get; }

    /// <summary>
    /// The offset time in beats (onset + duration).
    /// </summary>
    Rational OffsetBeats => OnsetBeats + DurationBeats;
}
