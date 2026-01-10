namespace StaffSharp.Abc.Importing;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Parses ABC notation (v2.1 standard) into a NotationScore.
/// https://abcnotation.com/wiki/abc:standard:v2.1
/// </summary>
public static partial class AbcParser
{
    public static NotationScore Parse(string abcContent)
    {
        ArgumentNullException.ThrowIfNull(abcContent);

        var lines = abcContent.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        // Parse headers
        string? referenceNumber = null;
        string? title = null;
        string? composer = null;
        var timeSignature = TimeSignature.CommonTime;
        var keySignature = KeySignature.C;
        int tempo = 120;
        var defaultNoteLength = Rational.Create(1, 8); // Default to eighth note
        var voiceData = new Dictionary<int, List<string>>(); // voiceNumber -> note lines
        int currentVoice = 1; // Default voice
        bool headerComplete = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("X:", StringComparison.Ordinal))
            {
                referenceNumber = line[2..].Trim();
            }
            else if (line.StartsWith("T:", StringComparison.Ordinal))
            {
                title = line[2..].Trim();
            }
            else if (line.StartsWith("C:", StringComparison.Ordinal))
            {
                composer = line[2..].Trim();
            }
            else if (line.StartsWith("M:", StringComparison.Ordinal))
            {
                timeSignature = AbcHeaderParser.ParseTimeSignature(line[2..].Trim());
            }
            else if (line.StartsWith("L:", StringComparison.Ordinal))
            {
                defaultNoteLength = AbcHeaderParser.ParseDefaultNoteLength(line[2..].Trim());
            }
            else if (line.StartsWith("Q:", StringComparison.Ordinal))
            {
                tempo = AbcHeaderParser.ParseTempo(line[2..].Trim());
            }
            else if (line.StartsWith("K:", StringComparison.Ordinal))
            {
                keySignature = AbcHeaderParser.ParseKeySignature(line[2..].Trim());
                headerComplete = true; // K: must be last header field
            }
            else if (line.StartsWith("V:", StringComparison.Ordinal) && headerComplete)
            {
                // Voice directive - switch to a different voice
                if (int.TryParse(line[2..].Trim(), out var voiceNumber))
                {
                    currentVoice = voiceNumber;
                }
            }
            else if (headerComplete)
            {
                // After K: header, all remaining lines are note content
                if (!voiceData.TryGetValue(currentVoice, out var noteLines))
                {
                    noteLines = [];
                    voiceData[currentVoice] = noteLines;
                }
                noteLines.Add(line);
            }
        }

        // If no voices were explicitly declared, use voice 1
        if (voiceData.Count == 0)
        {
            voiceData[1] = [];
        }

        // Parse each voice's notes into measures
        var voices = new List<Voice>();
        foreach (var (voiceNumber, noteLines) in voiceData.OrderBy(kvp => kvp.Key))
        {
            var measures = ParseNotes(string.Join(" ", noteLines), defaultNoteLength, keySignature);
            if (measures.Count > 0)
            {
                voices.Add(new Voice(voiceNumber, measures));
            }
        }

        // If no voices have content, create an empty voice 1
        if (voices.Count == 0)
        {
            voices.Add(new Voice(1, []));
        }

        // Build score
        var metadata = new ScoreMetadata(title, composer, keySignature, timeSignature, tempo);
        var part = new Part("Melody", Clef.Treble, voices);

        // Build TieSpans and SlurSpans from markers on notes
        BuildTieSpans(voices, part);
        BuildSlurSpans(voices, part);

        return new NotationScore(metadata, [part]);
    }

    /// <summary>
    /// Generic helper for building span objects from start/stop marker pairs across measures.
    /// Handles the common pattern of iterating voices/measures/events and matching marker pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of key used to match start and stop markers (e.g., Pitch for ties, int for slurs).</typeparam>
    /// <typeparam name="TMarkerData">Additional data to carry from start marker to span creation.</typeparam>
    private static void BuildSpansFromMarkers<TKey, TMarkerData>(
        List<Voice> voices,
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

    /// <summary>
    /// Builds TieSpan objects from TieMarkers on notes across all voices.
    /// </summary>
    private static void BuildTieSpans(List<Voice> voices, Part part)
    {
        BuildSpansFromMarkers<Pitch, TieMarker>(
            voices,
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
                    StartStaffNumber: 1,
                    EndStaffNumber: 1,
                    StartVoiceNumber: voice.Number,
                    EndVoiceNumber: voice.Number));
            });
    }

    /// <summary>
    /// Builds SlurSpan objects from SlurMarkers on notes across all voices.
    /// </summary>
    private static void BuildSlurSpans(List<Voice> voices, Part part)
    {
        BuildSpansFromMarkers<int, SlurMarker>(
            voices,
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
                        StartStaffNumber: 1,
                        EndStaffNumber: 1,
                        StartVoiceNumber: voice.Number,
                        EndVoiceNumber: voice.Number));
                }
            });
    }

    private static List<Measure> ParseNotes(
        string noteContent,
        Rational defaultNoteLength,
        KeySignature keySignature)
    {
        var measures = new List<Measure>();
        var globalEvents = new List<INotationEvent>();
        var slurTracker = new SlurTracker();
        var measureNumber = 1;
        var currentMeasureContent = new System.Text.StringBuilder();
        int index = 0;
        List<int>? nextMeasureVariants = null;
        BarlineType? nextMeasureStartBarline = null;

        while (index < noteContent.Length)
        {
            char c = noteContent[index];

            // Check for barline with potential repeat variant
            // Note: '[' is only a barline if followed by digit ([1, [2) or pipe ([|)
            // Also check for ':' which can start barlines like ':|' or '::' (but not '[K:' or '[M:' inline headers)
            bool isBarline = c == '|' ||
                (c == ':' && index + 1 < noteContent.Length && (noteContent[index + 1] == '|' || noteContent[index + 1] == ':')) ||
                (c == '[' && index + 1 < noteContent.Length && (char.IsDigit(noteContent[index + 1]) || noteContent[index + 1] == '|'));

            if (isBarline)
            {
                BarlineType? endBarline = null;
                BarlineType? nextStartBarline = null;
                List<int>? repeatVariants = null;

                // Handle barline and determine its type
                if (c == '[' && index + 1 < noteContent.Length && char.IsDigit(noteContent[index + 1]))
                {
                    // [1, [2, etc. - just a repeat variant marker, not an actual barline
                    index++; // Skip '['
                    repeatVariants = ParseRepeatVariants(noteContent, ref index);
                    // No barline type set
                }
                else if (c == '|' && index + 1 < noteContent.Length && char.IsDigit(noteContent[index + 1]))
                {
                    // |1, |2, etc. - normal barline followed by repeat variant
                    endBarline = BarlineType.Normal;
                    index++; // Skip |
                    repeatVariants = ParseRepeatVariants(noteContent, ref index);
                }
                else
                {
                    // Actual barline - collect and parse it
                    var barlineStr = CollectBarlineString(noteContent, ref index);
                    (endBarline, nextStartBarline) = ParseBarlineTypes(barlineStr);

                    // Check if there's a repeat variant after the barline
                    if (index < noteContent.Length && char.IsDigit(noteContent[index]))
                    {
                        repeatVariants = ParseRepeatVariants(noteContent, ref index);
                    }
                }

                // Create measure from accumulated content
                var measureString = currentMeasureContent.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(measureString))
                {
                    var events = ParseMeasureEvents(measureString, defaultNoteLength, keySignature, slurTracker, globalEvents);
                    if (events.Count > 0)
                    {
                        measures.Add(new Measure(
                            measureNumber++,
                            events,
                            repeatVariants: nextMeasureVariants,
                            startBarline: nextMeasureStartBarline,
                            endBarline: endBarline));
                    }
                }
                currentMeasureContent.Clear();

                // Store for next measure
                nextMeasureVariants = repeatVariants;
                nextMeasureStartBarline = nextStartBarline;
            }
            else
            {
                currentMeasureContent.Append(c);
                index++;
            }
        }

        // Handle final measure if any content remains
        var finalMeasureString = currentMeasureContent.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(finalMeasureString))
        {
            var events = ParseMeasureEvents(finalMeasureString, defaultNoteLength, keySignature, slurTracker, globalEvents);
            if (events.Count > 0)
            {
                measures.Add(new Measure(
                    measureNumber++,
                    events,
                    repeatVariants: nextMeasureVariants,
                    startBarline: nextMeasureStartBarline));
            }
        }

        return measures;
    }

    private static string CollectBarlineString(string content, ref int index)
    {
        var sb = new System.Text.StringBuilder();
        char c = content[index];

        // Start with |, [, or :
        sb.Append(c);
        index++;

        // Collect following barline characters (|, :, ], and [| pattern)
        while (index < content.Length)
        {
            char next = content[index];
            if (next == '|' || next == ':' || next == ']')
            {
                sb.Append(next);
                index++;
            }
            else if (next == '[' && index + 1 < content.Length && content[index + 1] == '|')
            {
                sb.Append(next);
                index++;
            }
            else
            {
                break; // Not a barline character
            }
        }

        return sb.ToString();
    }

    private static (BarlineType End, BarlineType? NextStart) ParseBarlineTypes(string barline)
    {
        // ABC barline patterns (can start with |, [, or :):
        // |   - normal barline
        // ||  - double barline
        // |]  - final barline
        // |:  - repeat start
        // :|  - repeat end
        // :: or :|: - repeat both
        // [|  - also a barline variant (treat as normal)

        BarlineType end;
        BarlineType? nextStart = null;

        // Check for repeat end first (patterns with : before last char)
        bool hasRepeatEnd = barline.Contains(":|", StringComparison.Ordinal) || barline.StartsWith("::", StringComparison.Ordinal);
        // Check for repeat start (patterns with : after initial |)
        bool hasRepeatStart = barline.Contains("|:", StringComparison.Ordinal) || barline.StartsWith("::", StringComparison.Ordinal);

        if (hasRepeatEnd && hasRepeatStart)
        {
            // :: or :|: - both
            end = BarlineType.RepeatEnd;
            nextStart = BarlineType.RepeatStart;
        }
        else if (hasRepeatEnd)
        {
            // :| - repeat end
            end = BarlineType.RepeatEnd;
        }
        else if (hasRepeatStart)
        {
            // |: - repeat start
            // Previous measure ends normally, next starts repeat
            end = BarlineType.Normal;
            nextStart = BarlineType.RepeatStart;
        }
        else if (barline == "|]")
        {
            end = BarlineType.Final;
        }
        else if (barline == "||")
        {
            end = BarlineType.DoubleBar;
        }
        else
        {
            // |, [|, or other variants - treat as normal
            end = BarlineType.Normal;
        }

        return (end, nextStart);
    }

    private static List<int> ParseRepeatVariants(string input, ref int index)
    {
        var variants = new List<int>();
        var numberStart = index;

        // Parse first number
        while (index < input.Length && char.IsDigit(input[index]))
        {
            index++;
        }

        if (int.TryParse(input[numberStart..index], out var firstNumber))
        {
            variants.Add(firstNumber);
        }

        // Check for comma-separated additional numbers (e.g., [1,3)
        while (index < input.Length && input[index] == ',')
        {
            index++; // Skip comma
            numberStart = index;
            while (index < input.Length && char.IsDigit(input[index]))
            {
                index++;
            }

            if (int.TryParse(input[numberStart..index], out var additionalNumber))
            {
                variants.Add(additionalNumber);
            }
        }

        return variants;
    }

    private static List<INotationEvent> ParseMeasureEvents(
        string measureString,
        Rational defaultNoteLength,
        KeySignature keySignature,
        SlurTracker slurTracker,
        List<INotationEvent> globalEvents)
    {
        var events = new List<INotationEvent>();
        int index = 0;
        Rational? nextNoteMultiplier = null;
        Tuplet? activeTuplet = null;
        int tupletNotesRemaining = 0;
        var currentKeySignature = keySignature; // Track key signature changes within measure
        TimeSignature? inlineMeasureTimeSignature = null;

        while (index < measureString.Length)
        {
            // Skip whitespace
            while (index < measureString.Length && char.IsWhiteSpace(measureString[index]))
            {
                index++;
            }

            if (index >= measureString.Length)
            {
                break;
            }

            // Check for inline header [K:G], [M:3/4], etc.
            if (AbcInlineHeaderParser.IsInlineHeader(measureString, index))
            {
                if (AbcInlineHeaderParser.TryParseInlineHeader(
                    measureString,
                    ref index,
                    ref currentKeySignature,
                    ref inlineMeasureTimeSignature))
                {
                    continue;
                }
            }

            // Check for tuplet specifier (takes precedence over slur)
            if (measureString[index] == '(' && index + 1 < measureString.Length && char.IsDigit(measureString[index + 1]))
            {
                if (AbcTupletParser.TryParseTuplet(measureString, ref index, out var tuplet, out var noteCount))
                {
                    activeTuplet = tuplet;
                    tupletNotesRemaining = noteCount;
                    continue; // Move to next iteration to parse the tuplet notes
                }
            }

            // Check for slur start
            if (slurTracker.TryStartSlur(measureString, ref index))
            {
                continue;
            }

            // Check for slur end
            if (slurTracker.TryEndSlur(measureString, ref index))
            {
                // Apply Stop markers to the last event
                if (events.Count > 0)
                {
                    events[^1] = slurTracker.ApplyPendingStops(events[^1]);
                    globalEvents[^1] = events[^1];
                }
                continue;
            }

            if (AbcEventParser.TryParseEvent(measureString, ref index, defaultNoteLength, currentKeySignature, out var noteEvent))
            {
                // Apply broken rhythm multiplier from previous note if any
                if (nextNoteMultiplier != null)
                {
                    noteEvent = ApplyDurationMultiplier(noteEvent, nextNoteMultiplier.Value);
                    nextNoteMultiplier = null;
                }

                // Check for broken rhythm operator after this note
                if (AbcBrokenRhythmParser.TryParseBrokenRhythm(measureString, ref index, out var firstMultiplier, out var secondMultiplier))
                {
                    // Apply multiplier to current note
                    noteEvent = ApplyDurationMultiplier(noteEvent, firstMultiplier);
                    // Store multiplier for next note
                    nextNoteMultiplier = secondMultiplier;
                }

                // Apply tuplet if active
                if (activeTuplet != null)
                {
                    noteEvent = ApplyTuplet(noteEvent, activeTuplet);
                    tupletNotesRemaining--;

                    if (tupletNotesRemaining <= 0)
                    {
                        activeTuplet = null;
                    }
                }

                // Apply pending slur Start markers
                noteEvent = slurTracker.ApplyPendingStarts(noteEvent);

                events.Add(noteEvent);
                globalEvents.Add(noteEvent);
            }
            else
            {
                // Skip unknown character
                index++;
            }
        }

        // Post-process: resolve tie endings
        TieTracker.ResolveTieEndings(events);

        // Note: Slurs are now stored as markers on notes, not as separate Slur objects
        return events;
    }

    private static INotationEvent ApplyDurationMultiplier(INotationEvent noteEvent, Rational multiplier)
    {
        // Apply duration multiplier to note or chord
        return noteEvent switch
        {
            NotationNote note => note with
            {
                Duration = (note.Duration.ToBeats() * multiplier).FromRational()
            },
            Chord chord => chord with
            {
                Duration = (chord.Duration.ToBeats() * multiplier).FromRational()
            },
            Rest rest => rest with
            {
                Duration = (rest.Duration.ToBeats() * multiplier).FromRational()
            },
            _ => noteEvent
        };
    }

    private static INotationEvent ApplyTuplet(INotationEvent noteEvent, Tuplet tuplet)
    {
        // Apply tuplet to the duration
        return noteEvent switch
        {
            NotationNote note => note with
            {
                Duration = new SymbolicDuration(note.Duration.Base, note.Duration.Dots, tuplet)
            },
            Chord chord => chord with
            {
                Duration = new SymbolicDuration(chord.Duration.Base, chord.Duration.Dots, tuplet)
            },
            Rest rest => rest with
            {
                Duration = new SymbolicDuration(rest.Duration.Base, rest.Duration.Dots, tuplet)
            },
            _ => noteEvent
        };
    }
}
