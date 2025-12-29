namespace StaffSharp.Notation;

/// <summary>
/// Represents a musical note in notation.
/// </summary>
public record NotationNote(
    Pitch Pitch,
    SymbolicDuration Duration,
    Velocity Velocity,
    TieType Tie = TieType.None
) : INotationEvent
{
    public NotationNote(Pitch Pitch, SymbolicDuration Duration)
        : this(Pitch, Duration, Velocity.MezzoForte, TieType.None)
    {
    }
}
