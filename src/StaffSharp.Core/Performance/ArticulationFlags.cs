namespace StaffSharp.Performance;

/// <summary>
/// Articulation flags that affect both playback and notation.
/// These are detected from audio analysis or specified in symbolic sources.
/// </summary>
[Flags]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Flags suffix is appropriate for [Flags] enum")]
public enum ArticulationFlags
{
    /// <summary>
    /// No articulation markings.
    /// </summary>
    None = 0,

    /// <summary>
    /// Short, detached note (duration less than 50% of beat).
    /// </summary>
    Staccato = 1 << 0,

    /// <summary>
    /// Emphasized, louder than surrounding notes (velocity spike).
    /// </summary>
    Accent = 1 << 1,

    /// <summary>
    /// Held for full value, not detached.
    /// </summary>
    Tenuto = 1 << 2,

    /// <summary>
    /// Strongly accented, combination of accent and staccato.
    /// </summary>
    Marcato = 1 << 3,

    /// <summary>
    /// Hold longer than written value, pause before next note.
    /// </summary>
    Fermata = 1 << 4,

    /// <summary>
    /// Smooth connection to next note, no gap.
    /// </summary>
    Legato = 1 << 5
}
