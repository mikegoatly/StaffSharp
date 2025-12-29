namespace StaffSharp.Importers.Abc;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Parses ABC notation (v2.1 standard) into a NotationScore.
/// https://abcnotation.com/wiki/abc:standard:v2.1
/// </summary>
public static partial class AbcParser
{
    private static readonly FrozenSet<char> ValidNoteChars = new[] 
    { 
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'Z', 
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'z'
    }.ToFrozenSet();

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
                timeSignature = ParseTimeSignature(line[2..].Trim());
            }
            else if (line.StartsWith("L:", StringComparison.Ordinal))
            {
                defaultNoteLength = ParseDefaultNoteLength(line[2..].Trim());
            }
            else if (line.StartsWith("Q:", StringComparison.Ordinal))
            {
                tempo = ParseTempo(line[2..].Trim());
            }
            else if (line.StartsWith("K:", StringComparison.Ordinal))
            {
                keySignature = ParseKeySignature(line[2..].Trim());
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

    private static TimeSignature ParseTimeSignature(string value)
    {
        value = value.Trim();

        if (value == "C")
        {
            return TimeSignature.CommonTime;
        }

        if (value == "C|")
        {
            return new TimeSignature(2, 2); // Cut time
        }

        var parts = value.Split('/');
        if (parts.Length == 2 && int.TryParse(parts[0], out var numerator) && int.TryParse(parts[1], out var denominator))
        {
            return new TimeSignature(numerator, denominator);
        }

        return TimeSignature.CommonTime;
    }

    private static Rational ParseDefaultNoteLength(string value)
    {
        // Format: 1/4, 1/8, etc.
        // This represents the note duration, which needs to be converted to beats (quarter notes)
        var parts = value.Trim().Split('/');
        if (parts.Length == 2 && int.TryParse(parts[0], out var numerator) && int.TryParse(parts[1], out var denominator))
        {
            // Convert note duration to beats: a 1/8 note = 4/8 = 1/2 beat
            return Rational.Create(numerator * 4, denominator);
        }

        return Rational.Create(1, 2); // Default: 1/8 note = 1/2 beat
    }

    private static KeySignature ParseKeySignature(string value)
    {
        // Remove mode suffixes (major, minor, etc.) for now - just look at root
        var key = value.Trim()
            .Replace("major", "", StringComparison.OrdinalIgnoreCase)
            .Replace("minor", "", StringComparison.OrdinalIgnoreCase)
            .Replace("min", "", StringComparison.OrdinalIgnoreCase)
            .Replace("m", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToUpperInvariant();

        return key switch
        {
            "C" => KeySignature.C,
            "G" => KeySignature.G,
            "D" => KeySignature.D,
            "A" => KeySignature.A,
            "E" => KeySignature.E,
            "B" => KeySignature.B,
            "F#" or "FSHARP" => KeySignature.FSharp,
            "C#" or "CSHARP" => KeySignature.CSharp,
            "F" => KeySignature.F,
            "BB" or "BFLAT" => KeySignature.BFlat,
            "EB" or "EFLAT" => KeySignature.EFlat,
            "AB" or "AFLAT" => KeySignature.AFlat,
            "DB" or "DFLAT" => KeySignature.DFlat,
            "GB" or "GFLAT" => KeySignature.GFlat,
            "CB" or "CFLAT" => KeySignature.CFlat,
            _ => KeySignature.C
        };
    }

    private static int ParseTempo(string value)
    {
        // Simple: just look for a number
        var match = System.Text.RegularExpressions.Regex.Match(value, @"\d+");
        return match.Success && int.TryParse(match.Value, out var tempo) ? tempo : 120;
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

            if (TryParseNote(measureString, ref index, defaultNoteLength, keySignature, out var noteEvent))
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

    private static bool TryParseNote(string input, ref int index, Rational defaultNoteLength, KeySignature keySignature, [NotNullWhen(true)] out INotationEvent? noteEvent)
    {
        noteEvent = null;

        if (index >= input.Length)
        {
            return false;
        }

        var startIndex = index;

        // Parse accidental (^, _, =, ^^, __)
        Accidental? accidental = null;
        if (index < input.Length && input[index] == '^')
        {
            accidental = Accidental.Sharp;
            index++;
            if (index < input.Length && input[index] == '^') // Double sharp
            {
                index++; // For now, treat as single sharp
            }
        }
        else if (index < input.Length && input[index] == '_')
        {
            accidental = Accidental.Flat;
            index++;
            if (index < input.Length && input[index] == '_') // Double flat
            {
                index++; // For now, treat as single flat
            }
        }
        else if (index < input.Length && input[index] == '=')
        {
            accidental = Accidental.Natural;
            index++;
        }

        // Parse note letter (A-G, a-g)
        if (index >= input.Length)
        {
            index = startIndex;
            return false;
        }

        char noteChar = input[index];
        if (!ValidNoteChars.Contains(noteChar))
        {
            index = startIndex;
            return false;
        }

        var isLowerCase = char.IsLower(noteChar);
        var upperChar = char.ToUpperInvariant(noteChar);

        var pitchClass = upperChar switch
        {
            'C' => PitchClass.C,
            'D' => PitchClass.D,
            'E' => PitchClass.E,
            'F' => PitchClass.F,
            'G' => PitchClass.G,
            'A' => PitchClass.A,
            'B' => PitchClass.B,
            'Z' => (PitchClass?)null, // Rest
            _ => null
        };

        if (pitchClass == null && upperChar != 'Z')
        {
            index = startIndex;
            return false;
        }

        index++; // Move past note letter

        // Parse octave modifiers (commas lower, apostrophes raise)
        int octave = isLowerCase ? 5 : 4; // Default: uppercase = octave 4, lowercase = octave 5

        while (index < input.Length && input[index] == ',')
        {
            octave--;
            index++;
        }

        while (index < input.Length && input[index] == '\'')
        {
            octave++;
            index++;
        }

        // Parse duration modifier (2, /2, /, etc.)
        var duration = defaultNoteLength;

        if (index < input.Length && char.IsDigit(input[index]))
        {
            var digitStart = index;
            while (index < input.Length && char.IsDigit(input[index]))
            {
                index++;
            }

            if (int.TryParse(input[digitStart..index], out var multiplier))
            {
                duration = duration * Rational.Create(multiplier, 1);
            }
        }
        else if (index < input.Length && input[index] == '/')
        {
            index++;
            if (index < input.Length && char.IsDigit(input[index]))
            {
                var digitStart = index;
                while (index < input.Length && char.IsDigit(input[index]))
                {
                    index++;
                }

                if (int.TryParse(input[digitStart..index], out var divisor))
                {
                    duration = duration * Rational.Create(1, divisor);
                }
            }
            else
            {
                // Just "/" means /2
                duration = duration * Rational.Create(1, 2);
            }
        }

        // Convert duration to SymbolicDuration
        var symbolicDuration = duration.FromRational();

        // Create pitch (or rest)
        if (pitchClass != null)
        {
            var pitch = new Pitch(pitchClass.Value, octave, accidental);
            noteEvent = new NotationNote(pitch, symbolicDuration);
            return true;
        }
        else if (upperChar == 'Z')
        {
            // Create rest
            noteEvent = new Rest(symbolicDuration);
            return true;
        }

        return false;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\|+\]?|\[\|")]
    private static partial System.Text.RegularExpressions.Regex BarLineRegex();
}
