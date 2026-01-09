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
        TieMarker? tieMarker = null,
        GraceNote? graceNote = null,
        IReadOnlyList<Decoration>? decorations = null,
        ChordSymbol? chordSymbol = null,
        Annotation? annotation = null,
        IReadOnlyList<SlurMarker>? slurMarkers = null)
    {
        ArgumentNullException.ThrowIfNull(pitches);

        if (pitches.Count < 2)
        {
            throw new ArgumentException("Chord must contain at least 2 pitches", nameof(pitches));
        }

        Pitches = pitches;
        Duration = duration;
        Velocity = velocity;
        TieMarker = tieMarker;
        GraceNote = graceNote;
        Decorations = decorations ?? [];
        ChordSymbol = chordSymbol;
        Annotation = annotation;
        SlurMarkers = slurMarkers ?? [];
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
    public TieMarker? TieMarker { get; }
    public GraceNote? GraceNote { get; }
    public IReadOnlyList<Decoration> Decorations { get; }
    public ChordSymbol? ChordSymbol { get; }
    public Annotation? Annotation { get; }
    public IReadOnlyList<SlurMarker> SlurMarkers { get; }
}
