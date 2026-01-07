namespace StaffSharp.Abc.Importing;

using StaffSharp.Notation;

/// <summary>
/// Tracks slur state while parsing a measure.
/// Slurs: (ABC) - notes within parentheses are slurred together
/// Nested slurs: (A(BC)D) - multiple slurs can be nested
/// Dotted slurs: .(ABC) - slight separation while maintaining phrasing
/// </summary>
internal sealed class SlurTracker
{
    private readonly Stack<SlurInfo> _activeSlurs = new();

    private sealed class SlurInfo
    {
        public int StartEventIndex { get; set; }
        public bool IsDotted { get; set; }
    }

    /// <summary>
    /// Checks if the current character starts a slur and consumes it.
    /// </summary>
    public bool TryStartSlur(string input, ref int index)
    {
        if (index >= input.Length)
        {
            return false;
        }

        // Check for dotted slur .(
        bool isDotted = false;
        if (input[index] == '.' && index + 1 < input.Length && input[index + 1] == '(')
        {
            isDotted = true;
            index++; // Consume '.'
        }

        if (index >= input.Length || input[index] != '(')
        {
            return false;
        }

        _activeSlurs.Push(new SlurInfo
        {
            StartEventIndex = -1, // Will be set when the next event is added
            IsDotted = isDotted
        });

        index++; // Consume '('
        return true;
    }

    /// <summary>
    /// Marks that an event was added at the given index.
    /// Updates slur tracking for events within active slurs.
    /// </summary>
    public void NotifyEventAdded(int eventIndex)
    {
        // Update all active slurs to include this event
        foreach (var slur in _activeSlurs)
        {
            if (slur.StartEventIndex == -1)
            {
                slur.StartEventIndex = eventIndex;
            }
        }
    }

    /// <summary>
    /// Checks if the current character ends a slur and creates the slur object.
    /// </summary>
    public bool TryEndSlur(string input, ref int index, IReadOnlyList<INotationEvent> allEvents, out Slur? slur)
    {
        slur = null;

        if (index >= input.Length || input[index] != ')')
        {
            return false;
        }

        if (_activeSlurs.Count == 0)
        {
            // Unmatched closing parenthesis - skip it
            index++;
            return false;
        }

        var slurInfo = _activeSlurs.Pop();

        if (slurInfo.StartEventIndex == -1 || slurInfo.StartEventIndex >= allEvents.Count)
        {
            // No events in this slur
            index++;
            return false;
        }

        // Gather events from start to current
        int endEventIndex = allEvents.Count - 1;
        int eventCount = endEventIndex - slurInfo.StartEventIndex + 1;

        if (eventCount < 2)
        {
            // Slur needs at least 2 events
            index++;
            return false;
        }

        var slurredEvents = new List<INotationEvent>();
        for (int i = slurInfo.StartEventIndex; i <= endEventIndex; i++)
        {
            slurredEvents.Add(allEvents[i]);
        }

        slur = new Slur(slurredEvents, slurInfo.IsDotted);
        index++; // Consume ')'
        return true;
    }

    /// <summary>
    /// Gets all slurs that were successfully created.
    /// </summary>
    public bool HasActiveSlurs() => _activeSlurs.Count > 0;
}
