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
        IReadOnlyList<Lyric>? lyrics = null)
    {
        Number = number;
        Events = events;
        TimeSignature = timeSignature;
        RepeatVariants = repeatVariants ?? Array.Empty<int>();
        Slurs = slurs ?? Array.Empty<Slur>();
        Lyrics = lyrics ?? Array.Empty<Lyric>();
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
}
