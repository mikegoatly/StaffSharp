namespace StaffSharp.Notation;

/// <summary>
/// Represents a measure (bar) of music.
/// </summary>
public class Measure
{
    public Measure(int number, IReadOnlyList<INotationEvent> events, TimeSignature? timeSignature = null)
    {
        Number = number;
        Events = events;
        TimeSignature = timeSignature;
    }

    public int Number { get; }
    public TimeSignature? TimeSignature { get; }
    public IReadOnlyList<INotationEvent> Events { get; }
}
