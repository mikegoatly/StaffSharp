namespace StaffSharp.Performance;

/// <summary>
/// Top-level container for a performance timeline (IR1).
/// This is the canonical representation of musical content from audio or real-time sources.
/// Represents "what plays when" as a flat list of events with exact rational timing.
/// </summary>
public sealed class PerformanceTimeline
{
    /// <summary>
    /// Creates a new performance timeline.
    /// </summary>
    /// <param name="tempoMap">Tempo and time signature information for time conversion.</param>
    /// <param name="events">All performance events (notes, etc.). Will be sorted by onset time.</param>
    /// <param name="metadata">Metadata about the performance (title, composer, source file, etc.).</param>
    public PerformanceTimeline(
        TempoMap tempoMap,
        IEnumerable<IPerformanceEvent> events,
        PerformanceMetadata? metadata = null)
    {
        TempoMap = tempoMap ?? throw new ArgumentNullException(nameof(tempoMap));
        Events = events.OrderBy(e => e.OnsetBeats).ToList();
        Metadata = metadata ?? new PerformanceMetadata();
    }

    /// <summary>
    /// Tempo and time signature information for converting between real time and musical time.
    /// </summary>
    public TempoMap TempoMap { get; }

    /// <summary>
    /// All performance events, sorted by onset time (earliest first).
    /// </summary>
    public IReadOnlyList<IPerformanceEvent> Events { get; }

    /// <summary>
    /// Metadata about the performance (title, composer, recording date, etc.).
    /// </summary>
    public PerformanceMetadata Metadata { get; }

    /// <summary>
    /// Queries all events that occur within a specific time range.
    /// Useful for rendering, playback, or analysis of specific sections.
    /// </summary>
    /// <param name="startBeats">Start of the time range (inclusive).</param>
    /// <param name="endBeats">End of the time range (exclusive).</param>
    /// <returns>All events that start within the specified range.</returns>
    public IEnumerable<IPerformanceEvent> EventsInRange(Rational startBeats, Rational endBeats)
    {
        // Binary search for efficiency (events are sorted)
        int startIndex = BinarySearchOnsetIndex(startBeats);

        for (int i = startIndex; i < Events.Count; i++)
        {
            var evt = Events[i];
            if (evt.OnsetBeats >= endBeats)
            {
                yield break;
            }
            if (evt.OnsetBeats >= startBeats)
            {
                yield return evt;
            }
        }
    }

    /// <summary>
    /// Gets all events that are sounding (active) at a specific musical time.
    /// An event is active if: onset <= time < offset.
    /// </summary>
    /// <param name="beats">The musical time to query.</param>
    /// <returns>All events that are sounding at that time.</returns>
    public IEnumerable<IPerformanceEvent> EventsAt(Rational beats)
    {
        foreach (var evt in Events)
        {
            // For QuantizedNoteEvent and SymbolicNoteEvent, check if the note is still sounding
            Rational offset = evt.OnsetBeats;

            if (evt is QuantizedNoteEvent quantized)
            {
                offset = quantized.OffsetBeats;
            }
            else if (evt is SymbolicNoteEvent symbolic)
            {
                offset = symbolic.OffsetBeats;
            }

            // Event is active if: onset <= beats < offset
            if (evt.OnsetBeats <= beats && beats < offset)
            {
                yield return evt;
            }
        }
    }

    /// <summary>
    /// Gets the total duration of the performance in beats.
    /// </summary>
    public Rational TotalDurationBeats
    {
        get
        {
            if (Events.Count == 0)
            {
                return Rational.Zero;
            }

            Rational maxOffset = Rational.Zero;

            foreach (var evt in Events)
            {
                Rational offset = evt.OnsetBeats;

                if (evt is QuantizedNoteEvent quantized)
                {
                    offset = quantized.OffsetBeats;
                }
                else if (evt is SymbolicNoteEvent symbolic)
                {
                    offset = symbolic.OffsetBeats;
                }

                if (offset > maxOffset)
                {
                    maxOffset = offset;
                }
            }

            return maxOffset;
        }
    }

    /// <summary>
    /// Gets the total duration of the performance in seconds.
    /// </summary>
    public double TotalDurationSeconds => TempoMap.BeatsToSeconds(TotalDurationBeats);

    /// <summary>
    /// Binary search to find the index of the first event at or after the specified onset time.
    /// </summary>
    private int BinarySearchOnsetIndex(Rational targetBeats)
    {
        int left = 0;
        int right = Events.Count;

        while (left < right)
        {
            int mid = left + (right - left) / 2;

            if (Events[mid].OnsetBeats < targetBeats)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        return left;
    }
}
