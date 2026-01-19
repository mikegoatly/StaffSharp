using StaffSharp.Notation;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Utility for building TieSpan and SlurSpan objects from markers on notes.
/// </summary>
public static class SpanBuilder
{
    /// <summary>
    /// Builds TieSpan objects from TieMarkers on notes across all voices in a part.
    /// </summary>
    public static void BuildTieSpans(Part part)
    {
        ArgumentNullException.ThrowIfNull(part);

        foreach (var staff in part.Staves)
        {
            BuildTieSpansForStaff(part, staff);
        }
    }

    /// <summary>
    /// Builds SlurSpan objects from SlurMarkers on notes across all voices in a part.
    /// </summary>
    public static void BuildSlurSpans(Part part)
    {
        ArgumentNullException.ThrowIfNull(part);

        foreach (var staff in part.Staves)
        {
            BuildSlurSpansForStaff(part, staff);
        }
    }

    private static void BuildTieSpansForStaff(Part part, Staff staff)
    {
        BuildSpansFromMarkers<Pitch, TieMarker>(
            staff.Voices,
            extractMarkers: noteEvent =>
            {
                if (noteEvent switch
                {
                    NotationNote note => (note.Pitch, note.TieMarker),
                    Chord chord => (chord.Pitches.ElementAtOrDefault(0), chord.TieMarker),
                    _ => (pitch: (Pitch?)null, tie: (TieMarker?)null)
                } is { pitch: { } pitch, tie: { } tie })
                {
                    return [
                        (
                        Key: pitch,
                        HasStart: tie.Type is TieMarkerType.Start or TieMarkerType.Both,
                        HasStop: tie.Type is TieMarkerType.Stop or TieMarkerType.Both,
                        Data: tie
                    )
                    ];
                }
                return [];
            },
            onMatch: (startEvent, endEvent, voice, pitch, startMarker, endMarker) =>
            {
                part.Ties.Add(new TieSpan(
                    startEvent,
                    endEvent,
                    StartStaffNumber: staff.Number,
                    EndStaffNumber: staff.Number,
                    StartVoiceNumber: voice.Number,
                    EndVoiceNumber: voice.Number));
            });
    }

    private static void BuildSlurSpansForStaff(Part part, Staff staff)
    {
        BuildSpansFromMarkers<int, SlurMarker>(
            staff.Voices,
            extractMarkers: noteEvent =>
            {
                var markers = noteEvent switch
                {
                    NotationNote note => note.SlurMarkers,
                    Chord chord => chord.SlurMarkers,
                    _ => []
                };

                return markers.Select(m => (
                    Key: m.Number,
                    HasStart: m.Type == SlurMarkerType.Start,
                    HasStop: m.Type == SlurMarkerType.Stop,
                    Data: m
                ));
            },
            onMatch: (startEvent, endEvent, voice, number, startMarker, endMarker) =>
            {
                // Only create SlurSpan if start and end are different events (ignore single-note slurs)
                if (!ReferenceEquals(startEvent, endEvent))
                {
                    part.Slurs.Add(new SlurSpan(
                        startEvent,
                        endEvent,
                        Number: number,
                        IsDotted: startMarker.IsDotted || endMarker.IsDotted,
                        StartStaffNumber: staff.Number,
                        EndStaffNumber: staff.Number,
                        StartVoiceNumber: voice.Number,
                        EndVoiceNumber: voice.Number));
                }
            });
    }

    /// <summary>
    /// Generic helper for building span objects from start/stop marker pairs across measures.
    /// Handles the common pattern of iterating voices/measures/events and matching marker pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of key used to match start and stop markers (e.g., Pitch for ties, int for slurs).</typeparam>
    /// <typeparam name="TMarkerData">Additional data to carry from start marker to span creation.</typeparam>
    private static void BuildSpansFromMarkers<TKey, TMarkerData>(
        IReadOnlyList<Voice> voices,
        Func<INotationEvent, IEnumerable<(TKey Key, bool HasStart, bool HasStop, TMarkerData Data)>> extractMarkers,
        Action<INotationEvent, INotationEvent, Voice, TKey, TMarkerData, TMarkerData> onMatch)
        where TKey : notnull
    {
        foreach (var voice in voices)
        {
            var pendingStarts = new Dictionary<TKey, (INotationEvent Event, TMarkerData Data)>();

            foreach (var noteEvent in voice.Measures.SelectMany(m => m.Events))
            {
                foreach (var (key, hasStart, hasStop, data) in extractMarkers(noteEvent))
                {
                    // Process stop first (for tie chains where Both means end previous + start next)
                    if (hasStop && pendingStarts.TryGetValue(key, out var startInfo))
                    {
                        onMatch(startInfo.Event, noteEvent, voice, key, startInfo.Data, data);
                        pendingStarts.Remove(key);
                    }

                    // Then process start
                    if (hasStart)
                    {
                        pendingStarts[key] = (noteEvent, data);
                    }
                }
            }
        }
    }
}
