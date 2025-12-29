namespace StaffSharp.Notation;

/// <summary>
/// Represents a grace note or grace note sequence.
/// Grace notes are ornamental notes that don't count toward the measure's time.
/// </summary>
/// <param name="Pitches">
/// The pitches of the grace notes.
/// </param>
/// <param name="IsAcciaccatura">
/// True for acciaccatura (slashed grace note), false for appoggiatura.
/// ABC notation: {/g}A = acciaccatura, {g}A = appoggiatura
/// </param>
public readonly record struct GraceNote(IReadOnlyList<Pitch> Pitches, bool IsAcciaccatura = false)
{
    /// <summary>
    /// Convenience factory method for creating a grace note with a single pitch.
    /// </summary>
    public static GraceNote FromPitch(Pitch pitch, bool isAcciaccatura = false) =>
        new([pitch], isAcciaccatura);
}
