namespace StaffSharp.Core;

/// <summary>
/// Represents a stream of musical events organized by voice, with optional tempo information.
/// This is the first intermediate representation - a raw event stream similar to MIDI.
/// </summary>
public class MusicalEventStream
{
    /// <summary>
    /// Creates a musical event stream.
    /// </summary>
    /// <param name="voices">The voices/channels/tracks containing note events.</param>
    /// <param name="tempo">Optional tempo information. May be null if tempo is unknown or variable.</param>
    public MusicalEventStream(IEnumerable<Voice> voices, Tempo? tempo = null)
    {
        Voices = voices?.ToList() ?? throw new ArgumentNullException(nameof(voices));
        Tempo = tempo;

        if (Voices.Count == 0)
        {
            throw new ArgumentException("Musical event stream must contain at least one voice.", nameof(voices));
        }

        // Validate unique voice IDs
        var voiceIds = Voices.Select(v => v.Id).ToList();
        if (voiceIds.Count != voiceIds.Distinct().Count())
        {
            throw new ArgumentException("Voice IDs must be unique.", nameof(voices));
        }
    }

    /// <summary>
    /// The voices in this stream.
    /// </summary>
    public IReadOnlyList<Voice> Voices { get; }

    /// <summary>
    /// Optional tempo information. May be null if tempo is unknown or will be inferred later.
    /// </summary>
    public Tempo? Tempo { get; }

    /// <summary>
    /// Gets the total duration of the stream (the longest voice duration).
    /// </summary>
    public TimeSpan TotalDuration => Voices.Count > 0
        ? Voices.Max(v => v.Duration)
        : TimeSpan.Zero;

    /// <summary>
    /// Gets the total number of note events across all voices.
    /// </summary>
    public int TotalEventCount => Voices.Sum(v => v.EventCount);

    /// <summary>
    /// Gets all note events from all voices, ordered by onset time.
    /// </summary>
    public IEnumerable<NoteEvent> GetAllEvents() =>
        Voices.SelectMany(v => v.Events).OrderBy(e => e.Onset);

    /// <summary>
    /// Gets a specific voice by ID.
    /// </summary>
    /// <param name="voiceId">The voice ID to retrieve.</param>
    /// <returns>The voice with the specified ID.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when voice ID is not found.</exception>
    public Voice GetVoice(int voiceId)
    {
        var voice = Voices.FirstOrDefault(v => v.Id == voiceId);
        if (voice == null)
        {
            throw new KeyNotFoundException($"Voice with ID {voiceId} not found.");
        }
        return voice;
    }

    /// <summary>
    /// Creates a monophonic stream with a single voice.
    /// </summary>
    /// <param name="events">The note events for the single voice.</param>
    /// <param name="tempo">Optional tempo information.</param>
    /// <param name="voiceName">Optional name for the voice.</param>
    public static MusicalEventStream CreateMonophonic(
        IEnumerable<NoteEvent> events,
        Tempo? tempo = null,
        string? voiceName = null)
    {
        var voice = new Voice(0, events, voiceName);
        return new MusicalEventStream(new[] { voice }, tempo);
    }

    /// <summary>
    /// Creates an empty stream with no events (useful for testing or as a placeholder).
    /// </summary>
    public static MusicalEventStream CreateEmpty(Tempo? tempo = null)
    {
        return new MusicalEventStream(new[] { Voice.Empty(0) }, tempo);
    }
}
