namespace StaffSharp.Core;

/// <summary>
/// Represents a single voice (or channel/track) containing a sequence of musical note events.
/// </summary>
public class Voice
{
    /// <summary>
    /// Creates a voice with a collection of note events.
    /// </summary>
    /// <param name="id">Unique identifier for this voice (e.g., MIDI channel, track number).</param>
    /// <param name="events">The note events in this voice, typically ordered by onset time.</param>
    /// <param name="name">Optional human-readable name for the voice (e.g., "Piano", "Vocals").</param>
    public Voice(int id, IEnumerable<NoteEvent> events, string? name = null)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Voice ID must be non-negative.");
        }

        Id = id;
        Name = name;
        Events = events?.OrderBy(e => e.Onset).ToList() ?? throw new ArgumentNullException(nameof(events));
    }

    /// <summary>
    /// Unique identifier for this voice.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Optional human-readable name for the voice.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The note events in this voice, ordered by onset time.
    /// </summary>
    public IReadOnlyList<NoteEvent> Events { get; }

    /// <summary>
    /// Gets the total duration of this voice (time from start to the end of the last note).
    /// </summary>
    public TimeSpan Duration => Events.Count > 0
        ? Events.Max(e => e.Offset)
        : TimeSpan.Zero;

    /// <summary>
    /// Gets the number of note events in this voice.
    /// </summary>
    public int EventCount => Events.Count;

    /// <summary>
    /// Creates an empty voice with the specified ID.
    /// </summary>
    public static Voice Empty(int id, string? name = null) => new(id, Array.Empty<NoteEvent>(), name);
}
