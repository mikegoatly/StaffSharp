namespace StaffSharp.Notation;

/// <summary>
/// Represents a measure (bar) of music.
/// </summary>
public class Measure
{
    public Measure(
        int number,
        IReadOnlyList<INotationEvent> events,
        TimeSignature? timeSignature = null,
        IReadOnlyList<int>? repeatVariants = null,
        IReadOnlyList<Slur>? slurs = null,
        IReadOnlyList<Lyric>? lyrics = null,
        BarlineType? startBarline = null,
        BarlineType? endBarline = null,
        IReadOnlyList<Direction>? directions = null)
    {
        Number = number;
        Events = events;
        TimeSignature = timeSignature;
        RepeatVariants = repeatVariants ?? Array.Empty<int>();
        Slurs = slurs ?? Array.Empty<Slur>();
        Lyrics = lyrics ?? Array.Empty<Lyric>();
        StartBarline = startBarline;
        EndBarline = endBarline;
        Directions = directions ?? Array.Empty<Direction>();
    }

    public int Number { get; }
    public TimeSignature? TimeSignature { get; }
    public IReadOnlyList<INotationEvent> Events { get; }

    /// <summary>
    /// Repeat variant numbers this measure belongs to (e.g., [1, [2, [1,3).
    /// Empty if this is not a repeat variant.
    /// ABC notation: |1 ... :|2 ... :|
    /// </summary>
    public IReadOnlyList<int> RepeatVariants { get; }

    /// <summary>
    /// Slurs that group events within this measure.
    /// </summary>
    public IReadOnlyList<Slur> Slurs { get; }

    /// <summary>
    /// Lyric lines associated with this measure.
    /// Multiple lyric lines represent different verses.
    /// </summary>
    public IReadOnlyList<Lyric> Lyrics { get; }

    /// <summary>
    /// The type of barline at the start of this measure (e.g., repeat start).
    /// </summary>
    public BarlineType? StartBarline { get; }

    /// <summary>
    /// The type of barline at the end of this measure (e.g., repeat end, double, final).
    /// </summary>
    public BarlineType? EndBarline { get; }

    /// <summary>
    /// Musical directions or expression markings for this measure
    /// (e.g., tempo, dynamics, rehearsal marks, text directions).
    /// </summary>
    public IReadOnlyList<Direction> Directions { get; }
}
