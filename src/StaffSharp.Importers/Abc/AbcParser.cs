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
        var noteLines = new List<string>();
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
            else if (headerComplete && line.AsSpan().IndexOf(':') == -1)
            {
                // This is a note line (after K: header)
                noteLines.Add(line);
            }
        }

        // Parse notes into measures
        var measures = ParseNotes(string.Join(" ", noteLines), defaultNoteLength, keySignature);

        // Build score
        var metadata = new ScoreMetadata(title, composer, keySignature, timeSignature, tempo);
        var voice = new Voice(1, measures);
        var part = new Part("Melody", Clef.Treble, [voice]);

        return new NotationScore(metadata, [part]);
    }

    private static List<Measure> ParseNotes(
        string noteContent,
        Rational defaultNoteLength,
        KeySignature keySignature)
    {
        var measures = new List<Measure>();
        var measureNumber = 1;

        // Split by barlines (|, ||, |], [|, |:, :|, ::)
        var measureStrings = BarLineRegex().Split(noteContent);

        foreach (var measureString in measureStrings)
        {
            if (string.IsNullOrWhiteSpace(measureString))
            {
                continue;
            }

            var events = ParseMeasureEvents(measureString.Trim(), defaultNoteLength, keySignature);

            if (events.Count > 0)
            {
                measures.Add(new Measure(measureNumber++, events));
            }
        }

        return measures;
    }

    private static List<INotationEvent> ParseMeasureEvents(string measureString, Rational defaultNoteLength, KeySignature keySignature)
    {
        var events = new List<INotationEvent>();
        int index = 0;

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

            if (AbcEventParser.TryParseEvent(measureString, ref index, defaultNoteLength, keySignature, out var noteEvent))
            {
                events.Add(noteEvent);
            }
            else
            {
                // Skip unknown character
                index++;
            }
        }

        return events;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\|+\]?|\[\|")]
    private static partial System.Text.RegularExpressions.Regex BarLineRegex();
}
