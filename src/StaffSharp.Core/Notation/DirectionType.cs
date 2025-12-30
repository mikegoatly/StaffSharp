namespace StaffSharp.Notation;

/// <summary>
/// Represents the type of musical direction or expression marking.
/// </summary>
public enum DirectionType
{
    /// <summary>
    /// Tempo marking (e.g., "Allegro", "Andante", with optional BPM).
    /// </summary>
    Tempo,

    /// <summary>
    /// Dynamic marking (e.g., "p", "f", "mf", crescendo, diminuendo).
    /// </summary>
    Dynamic,

    /// <summary>
    /// Rehearsal mark (e.g., "A", "B", "1", "2").
    /// </summary>
    RehearsalMark,

    /// <summary>
    /// Text direction (e.g., "D.C. al Coda", "Fine").
    /// </summary>
    Text,

    /// <summary>
    /// Pedal marking (sustain pedal down).
    /// </summary>
    Pedal,

    /// <summary>
    /// Crescendo (gradually getting louder).
    /// </summary>
    Crescendo,

    /// <summary>
    /// Diminuendo or decrescendo (gradually getting softer).
    /// </summary>
    Diminuendo
}
