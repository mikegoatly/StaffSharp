namespace StaffSharp.Notation;

/// <summary>
/// Represents a musical note in notation.
/// </summary>
public record NotationNote(
    Pitch Pitch,
    SymbolicDuration Duration,
    Velocity Velocity,
    TieMarker? TieMarker = null,
    GraceNote? GraceNote = null,
    IReadOnlyList<Decoration>? Decorations = null,
    ChordSymbol? ChordSymbol = null,
    Annotation? Annotation = null,
    IReadOnlyList<SlurMarker>? SlurMarkers = null
) : INotationEvent
{
    public NotationNote(Pitch Pitch, SymbolicDuration Duration)
        : this(Pitch, Duration, Velocity.MezzoForte, null, null, null, null, null, null)
    {
    }

    // Ensure Decorations is never null
    public IReadOnlyList<Decoration> Decorations { get; init; } = Decorations ?? [];

    // Ensure SlurMarkers is never null
    public IReadOnlyList<SlurMarker> SlurMarkers { get; init; } = SlurMarkers ?? [];
}
