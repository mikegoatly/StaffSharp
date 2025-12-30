namespace StaffSharp.Importers.Abc;

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

        return new NotationScore(metadata, [part]);
    }

    private static List<Measure> ParseNotes(
        string noteContent,
        Rational defaultNoteLength,
        KeySignature keySignature)
    {
        var measures = new List<Measure>();
        var measureNumber = 1;
        var currentMeasureContent = new System.Text.StringBuilder();
        int index = 0;
        List<int>? nextMeasureVariants = null;

        while (index < noteContent.Length)
        {
            char c = noteContent[index];

            // Check for barline with potential repeat variant
            // Note: '[' is only a barline if followed by digit ([1, [2) or pipe ([|)
            bool isBarline = c == '|' || (c == '[' && index + 1 < noteContent.Length && (char.IsDigit(noteContent[index + 1]) || noteContent[index + 1] == '|'));

            if (isBarline)
            {
                // First, create measure from accumulated content (if any)
                var measureString = currentMeasureContent.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(measureString))
                {
                    var (events, slurs) = ParseMeasureEvents(measureString, defaultNoteLength, keySignature);
                    if (events.Count > 0)
                    {
                        measures.Add(new Measure(measureNumber++, events, slurs: slurs, repeatVariants: nextMeasureVariants));
                        nextMeasureVariants = null; // Consumed the repeat variant
                    }
                }
                currentMeasureContent.Clear();

                // Now handle the barline and check for repeat variant marker
                List<int>? repeatVariants = null;
                if (c == '[' && index + 1 < noteContent.Length && char.IsDigit(noteContent[index + 1]))
                {
                    // Parse [1 or [2 or [1,3 etc.
                    index++; // Skip the '['
                    repeatVariants = ParseRepeatVariants(noteContent, ref index);
                }
                else if (c == '|' && index + 1 < noteContent.Length && char.IsDigit(noteContent[index + 1]))
                {
                    // Parse |1 or |2 etc.
                    index++; // Skip |
                    repeatVariants = ParseRepeatVariants(noteContent, ref index);
                }
                else
                {
                    // Regular barline - skip it
                    index++;
                    // Skip additional barline characters (||, |], |:, :|, ::, [|)
                    // But DON'T skip '[' if followed by a digit (that's a repeat variant marker)
                    while (index < noteContent.Length)
                    {
                        char nextChar = noteContent[index];
                        if (nextChar == '|' || nextChar == ']' || nextChar == ':')
                        {
                            index++;
                        }
                        else if (nextChar == '[' && index + 1 < noteContent.Length && noteContent[index + 1] == '|')
                        {
                            // [| is a barline
                            index++;
                        }
                        else
                        {
                            // Stop - might be a repeat variant marker or regular content
                            break;
                        }
                    }
                }

                // Store repeat variant for the NEXT measure
                if (repeatVariants != null)
                {
                    nextMeasureVariants = repeatVariants;
                }
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
            var (events, slurs) = ParseMeasureEvents(finalMeasureString, defaultNoteLength, keySignature);
            if (events.Count > 0)
            {
                measures.Add(new Measure(measureNumber++, events, slurs: slurs, repeatVariants: nextMeasureVariants));
            }
        }

        return measures;
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

    private static (List<INotationEvent> Events, List<Slur> Slurs) ParseMeasureEvents(string measureString, Rational defaultNoteLength, KeySignature keySignature)
    {
        var events = new List<INotationEvent>();
        var slurs = new List<Slur>();
        var slurTracker = new SlurTracker();
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
            if (slurTracker.TryEndSlur(measureString, ref index, events, out var slur) && slur != null)
            {
                slurs.Add(slur);
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

                events.Add(noteEvent);
                slurTracker.NotifyEventAdded(events.Count - 1);
            }
            else
            {
                // Skip unknown character
                index++;
            }
        }

        return (events, slurs);
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
            Chord chord => new Chord(
                chord.Pitches,
                (chord.Duration.ToBeats() * multiplier).FromRational(),
                chord.Velocity,
                chord.Tie,
                chord.GraceNote,
                chord.Decorations,
                chord.ChordSymbol,
                chord.Annotation),
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
            Chord chord => new Chord(
                chord.Pitches,
                new SymbolicDuration(chord.Duration.Base, chord.Duration.Dots, tuplet),
                chord.Velocity,
                chord.Tie,
                chord.GraceNote,
                chord.Decorations,
                chord.ChordSymbol,
                chord.Annotation),
            Rest rest => rest with
            {
                Duration = new SymbolicDuration(rest.Duration.Base, rest.Duration.Dots, tuplet)
            },
            _ => noteEvent
        };
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\|+\]?|\[\|")]
    private static partial System.Text.RegularExpressions.Regex BarLineRegex();
}
