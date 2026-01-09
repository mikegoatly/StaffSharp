namespace StaffSharp.Abc.Importing;

using StaffSharp.Notation;

/// <summary>
/// Tracks slur state while parsing a measure and applies slur markers to events.
/// Slurs: (ABC) - notes within parentheses are slurred together
/// Nested slurs: (A(BC)D) - multiple slurs can be nested (each gets a unique number)
/// Dotted slurs: .(ABC) - slight separation while maintaining phrasing
/// </summary>
internal sealed class SlurTracker
{
    private readonly Stack<SlurInfo> _activeSlurs = new();
    private int _nextSlurNumber = 1; // Assign unique numbers for nested slurs
    private readonly List<PendingStart> _pendingStarts = []; // Slur starts waiting for next event
    private readonly List<PendingStop> _pendingStops = []; // Slur stops to apply to last event

    private sealed class SlurInfo
    {
        public int Number { get; set; }
        public bool IsDotted { get; set; }
    }

    private readonly record struct PendingStart
    {
        public int Number { get; init; }
        public bool IsDotted { get; init; }
    }

    private readonly record struct PendingStop
    {
        public int Number { get; init; }
        public bool IsDotted { get; init; }
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

        var slurNumber = _nextSlurNumber++;
        _activeSlurs.Push(new SlurInfo
        {
            Number = slurNumber,
            IsDotted = isDotted
        });

        // Mark that the next event should get a Start marker
        _pendingStarts.Add(new PendingStart { Number = slurNumber, IsDotted = isDotted });

        index++; // Consume '('
        return true;
    }

    /// <summary>
    /// Checks if the current character ends a slur and consumes it.
    /// </summary>
    public bool TryEndSlur(string input, ref int index)
    {
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

        // Mark that the last event should get a Stop marker
        _pendingStops.Add(new PendingStop { Number = slurInfo.Number, IsDotted = slurInfo.IsDotted });

        index++; // Consume ')'
        return true;
    }

    /// <summary>
    /// Applies pending Start markers to an event and returns the updated event.
    /// Call this right after adding a new event.
    /// </summary>
    public INotationEvent ApplyPendingStarts(INotationEvent noteEvent)
    {
        if (_pendingStarts.Count == 0)
        {
            return noteEvent;
        }

        var markers = _pendingStarts
            .Select(ps => new SlurMarker(ps.Number, SlurMarkerType.Start, ps.IsDotted))
            .ToList();
        _pendingStarts.Clear();

        return noteEvent switch
        {
            NotationNote note => note with { SlurMarkers = markers },
            Chord chord => new Chord(
                chord.Pitches,
                chord.Duration,
                chord.Velocity,
                chord.TieMarker,
                chord.GraceNote,
                chord.Decorations,
                chord.ChordSymbol,
                chord.Annotation,
                markers),
            _ => noteEvent
        };
    }

    /// <summary>
    /// Applies pending Stop markers to the last event and returns the updated event.
    /// Call this when ending a slur.
    /// </summary>
    public INotationEvent ApplyPendingStops(INotationEvent noteEvent)
    {
        if (_pendingStops.Count == 0)
        {
            return noteEvent;
        }

        var stopMarkers = _pendingStops
            .Select(ps => new SlurMarker(ps.Number, SlurMarkerType.Stop, ps.IsDotted))
            .ToList();
        _pendingStops.Clear();

        // Merge with existing markers
        var existingMarkers = noteEvent switch
        {
            NotationNote note => note.SlurMarkers,
            Chord chord => chord.SlurMarkers,
            _ => []
        };

        var allMarkers = existingMarkers.Concat(stopMarkers).ToList();

        return noteEvent switch
        {
            NotationNote note => note with { SlurMarkers = allMarkers },
            Chord chord => new Chord(
                chord.Pitches,
                chord.Duration,
                chord.Velocity,
                chord.TieMarker,
                chord.GraceNote,
                chord.Decorations,
                chord.ChordSymbol,
                chord.Annotation,
                allMarkers),
            _ => noteEvent
        };
    }

    /// <summary>
    /// Checks if there are any pending stops to apply.
    /// </summary>
    public bool HasPendingStops() => _pendingStops.Count > 0;

    /// <summary>
    /// Checks if there are active slurs.
    /// </summary>
    public bool HasActiveSlurs() => _activeSlurs.Count > 0;
}
