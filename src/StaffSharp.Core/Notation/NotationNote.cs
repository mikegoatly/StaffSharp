namespace StaffSharp.Notation;

/// <summary>
/// Represents a musical note in notation.
/// </summary>
public record NotationNote(
    Pitch Pitch,
    SymbolicDuration Duration,
    Velocity Velocity,
    TieType Tie = TieType.None,
    GraceNote? GraceNote = null,
    IReadOnlyList<Decoration>? Decorations = null,
    ChordSymbol? ChordSymbol = null,
    Annotation? Annotation = null
) : INotationEvent
{
    public NotationNote(Pitch Pitch, SymbolicDuration Duration)
        : this(Pitch, Duration, Velocity.MezzoForte, TieType.None, null, null, null, null)
    {
    }

    // Ensure Decorations is never null
    public IReadOnlyList<Decoration> Decorations { get; init; } = Decorations ?? Array.Empty<Decoration>();
}
