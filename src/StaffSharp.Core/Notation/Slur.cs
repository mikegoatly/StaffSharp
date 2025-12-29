namespace StaffSharp.Notation;

/// <summary>
/// Represents a slur grouping of notation events.
/// Slurs indicate that notes should be played smoothly connected (legato).
/// ABC notation: (ABC) or (A(B)C) for nested slurs
/// </summary>
public record Slur
{
    public Slur(IReadOnlyList<INotationEvent> events, bool isDotted = false)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count < 2)
        {
            throw new ArgumentException("Slur must contain at least 2 events", nameof(events));
        }

        Events = events;
        IsDotted = isDotted;
    }

    /// <summary>
    /// The events grouped by this slur.
    /// </summary>
    public IReadOnlyList<INotationEvent> Events { get; }

    /// <summary>
    /// True if this is a dotted slur (ABC: .(...)).
    /// Dotted slurs indicate a slight separation while maintaining phrasing.
    /// </summary>
    public bool IsDotted { get; }
}
