namespace StaffSharp.Importers.Abc;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Parses ABC notation events (notes, chords, rests, decorations, etc.).
/// </summary>
internal static class AbcEventParser
{
    private static readonly FrozenSet<char> ValidNoteChars = new[]
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'z'
    }.ToFrozenSet();

    private static readonly FrozenDictionary<char, PitchClass?> PitchClasses = new Dictionary<char, PitchClass?>
    {
        ['A'] = PitchClass.A,
        ['B'] = PitchClass.B,
        ['C'] = PitchClass.C,
        ['D'] = PitchClass.D,
        ['E'] = PitchClass.E,
        ['F'] = PitchClass.F,
        ['G'] = PitchClass.G
    }.ToFrozenDictionary();

    /// <summary>
    /// Tries to parse a notation event (note, chord, or rest) at the current position.
    /// </summary>
    public static bool TryParseEvent(
        string input,
        ref int index,
        Rational defaultNoteLength,
        KeySignature keySignature,
        [NotNullWhen(true)] out INotationEvent? noteEvent)
    {
        noteEvent = null;

        if (index >= input.Length)
        {
            return false;
        }

        var startIndex = index;

        // Parse decorations (!trill!, ., ~, etc.)
        var decorations = AbcDecorationParser.ParseDecorations(input, ref index);

        // TODO: Parse chord symbols "Cmaj7"
        // TODO: Parse annotations "^text"

        // Parse grace notes {ABC}
        GraceNote? graceNote = null;
        if (input[index] == '{')
        {
            if (!AbcGraceNoteParser.TryParseGraceNote(input, ref index, keySignature, out graceNote))
            {
                index = startIndex;
                return false;
            }
        }

        // Check for chord [CEG]
        if (input[index] == '[')
        {
            return TryParseChord(input, ref index, defaultNoteLength, keySignature, graceNote, decorations, out noteEvent);
        }

        // Otherwise, parse single note or rest
        return TryParseNote(input, ref index, defaultNoteLength, keySignature, graceNote, decorations, out noteEvent);
    }

    private static bool TryParseNote(
        string input,
        ref int index,
        Rational defaultNoteLength,
        KeySignature keySignature,
        GraceNote? graceNote,
        List<Decoration> decorations,
        [NotNullWhen(true)] out INotationEvent? noteEvent)
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
            index++;
            if (index < input.Length && input[index] == '^') // Double sharp
            {
                accidental = Accidental.DoubleSharp;
                index++;
            }
            else if (index < input.Length && input[index] == '/') // Quarter sharp
            {
                accidental = Accidental.QuarterSharp;
                index++;
            }
            else if (index < input.Length - 1 && input[index] == '3' && input[index + 1] == '/') // Three-quarter sharp
            {
                accidental = Accidental.ThreeQuarterSharp;
                index += 2;
            }
            else
            {
                accidental = Accidental.Sharp;
            }
        }
        else if (index < input.Length && input[index] == '_')
        {
            index++;
            if (index < input.Length && input[index] == '_') // Double flat
            {
                accidental = Accidental.DoubleFlat;
                index++;
            }
            else if (index < input.Length && input[index] == '/') // Quarter flat
            {
                accidental = Accidental.QuarterFlat;
                index++;
            }
            else if (index < input.Length - 1 && input[index] == '3' && input[index + 1] == '/') // Three-quarter flat
            {
                accidental = Accidental.ThreeQuarterFlat;
                index += 2;
            }
            else
            {
                accidental = Accidental.Flat;
            }
        }
        else if (index < input.Length && input[index] == '=')
        {
            accidental = Accidental.Natural;
            index++;
        }

        // Parse note letter (A-G, a-g, Z, z)
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
        var duration = ParseDuration(input, ref index, defaultNoteLength);

        // Parse tie (-)
        TieType tie = TieType.None;
        if (index < input.Length && input[index] == '-')
        {
            tie = TieType.Start; // Mark as start of tie
            index++;
        }

        // Convert duration to SymbolicDuration
        var symbolicDuration = duration.FromRational();

        // Create pitch (or rest)
        if (pitchClass != null)
        {
            var pitch = new Pitch(pitchClass.Value, octave, accidental);
            noteEvent = new NotationNote(
                pitch,
                symbolicDuration,
                Velocity.MezzoForte,
                tie,
                graceNote,
                decorations.Count > 0 ? decorations : null);
            return true;
        }
        else if (upperChar == 'Z')
        {
            // Create rest (rests cannot be tied or have grace notes)
            noteEvent = new Rest(symbolicDuration);
            return true;
        }

        return false;
    }

    private static bool TryParseChord(
        string input,
        ref int index,
        Rational defaultNoteLength,
        KeySignature keySignature,
        GraceNote? graceNote,
        List<Decoration> decorations,
        [NotNullWhen(true)] out INotationEvent? noteEvent)
    {
        noteEvent = null;

        if (index >= input.Length || input[index] != '[')
        {
            return false;
        }

        var startIndex = index;
        index++; // Skip [

        var pitches = new List<Pitch>();

        // Parse notes within the chord
        while (index < input.Length && input[index] != ']')
        {
            // Skip whitespace
            while (index < input.Length && char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            if (index >= input.Length || input[index] == ']')
            {
                break;
            }

            // Parse individual note within chord
            if (!TryParseChordNote(input, ref index, out var pitch) || pitch == null)
            {
                index = startIndex;
                return false;
            }

            pitches.Add(pitch.Value);
        }

        if (index >= input.Length || input[index] != ']')
        {
            index = startIndex;
            return false;
        }

        index++; // Skip ]

        if (pitches.Count < 2)
        {
            index = startIndex;
            return false;
        }

        // Parse duration after the chord
        var duration = ParseDuration(input, ref index, defaultNoteLength);
        var symbolicDuration = duration.FromRational();

        // Parse tie (-)
        TieType tie = TieType.None;
        if (index < input.Length && input[index] == '-')
        {
            tie = TieType.Start;
            index++;
        }

        noteEvent = new Chord(
            pitches,
            symbolicDuration,
            Velocity.MezzoForte,
            tie,
            graceNote,
            decorations.Count > 0 ? decorations : null);
        return true;
    }

    private static bool TryParseChordNote(
        string input,
        ref int index,
        [NotNullWhen(true)] out Pitch? pitch)
    {
        pitch = null;

        if (index >= input.Length)
        {
            return false;
        }

        var startIndex = index;

        // Parse accidental
        Accidental? accidental = null;
        if (index < input.Length && input[index] == '^')
        {
            index++;
            if (index < input.Length && input[index] == '^')
            {
                accidental = Accidental.DoubleSharp;
                index++;
            }
            else
            {
                accidental = Accidental.Sharp;
            }
        }
        else if (index < input.Length && input[index] == '_')
        {
            index++;
            if (index < input.Length && input[index] == '_')
            {
                accidental = Accidental.DoubleFlat;
                index++;
            }
            else
            {
                accidental = Accidental.Flat;
            }
        }
        else if (index < input.Length && input[index] == '=')
        {
            accidental = Accidental.Natural;
            index++;
        }

        // Parse note letter
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

        var pitchClass = PitchClasses.GetValueOrDefault(upperChar, (PitchClass?)null);
        if (pitchClass is null)
        {
            index = startIndex;
            return false;
        }

        index++; // Move past note letter

        // Parse octave modifiers
        int octave = isLowerCase ? 5 : 4;

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

        // Note: Duration for individual notes within chord is parsed on the chord as a whole

        pitch = new Pitch(pitchClass.GetValueOrDefault(), octave, accidental);
        return true;
    }

    private static Rational ParseDuration(string input, ref int index, Rational defaultNoteLength)
    {
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

        return duration;
    }
}
