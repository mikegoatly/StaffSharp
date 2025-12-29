namespace StaffSharp.Notation;

/// <summary>
/// Represents a chord (multiple simultaneous pitches) in notation.
/// ABC notation: [CEG] = C major chord
/// </summary>
public record Chord : INotationEvent
{
    public Chord(
        IReadOnlyList<Pitch> pitches,
        SymbolicDuration duration,
        Velocity velocity,
        TieType tie = TieType.None,
        GraceNote? graceNote = null,
        IReadOnlyList<Decoration>? decorations = null,
        ChordSymbol? chordSymbol = null,
        Annotation? annotation = null)
    {
        ArgumentNullException.ThrowIfNull(pitches);

        if (pitches.Count < 2)
        {
            throw new ArgumentException("Chord must contain at least 2 pitches", nameof(pitches));
        }

        Pitches = pitches;
        Duration = duration;
        Velocity = velocity;
        Tie = tie;
        GraceNote = graceNote;
        Decorations = decorations ?? Array.Empty<Decoration>();
        ChordSymbol = chordSymbol;
        Annotation = annotation;
    }

    /// <summary>
    /// Convenience constructor with default velocity.
    /// </summary>
    public Chord(IReadOnlyList<Pitch> pitches, SymbolicDuration duration)
        : this(pitches, duration, Velocity.MezzoForte)
    {
    }

    public IReadOnlyList<Pitch> Pitches { get; }
    public SymbolicDuration Duration { get; }
    public Velocity Velocity { get; }
    public TieType Tie { get; }
    public GraceNote? GraceNote { get; }
    public IReadOnlyList<Decoration> Decorations { get; }
    public ChordSymbol? ChordSymbol { get; }
    public Annotation? Annotation { get; }
}
