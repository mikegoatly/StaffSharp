namespace StaffSharp.Importers.Abc;

using System.Diagnostics.CodeAnalysis;
using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Handles ABC grace note notation ({ABC}, {/ABC}).
/// </summary>
internal static class AbcGraceNoteParser
{
    /// <summary>
    /// Tries to parse a grace note sequence at the current position.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="index">Current position (will be advanced if grace notes found)</param>
    /// <param name="keySignature">The current key signature for applying accidentals</param>
    /// <param name="graceNote">The parsed grace note</param>
    /// <returns>True if a grace note sequence was found</returns>
    public static bool TryParseGraceNote(
        string input,
        ref int index,
        KeySignature keySignature,
        [NotNullWhen(true)] out GraceNote? graceNote)
    {
        graceNote = null;

        if (index >= input.Length || input[index] != '{')
        {
            return false;
        }

        var startIndex = index;
        index++; // Skip '{'

        // Check for acciaccatura (slash)
        bool isAcciaccatura = false;
        if (index < input.Length && input[index] == '/')
        {
            isAcciaccatura = true;
            index++;
        }

        var pitches = new List<Pitch>();

        // Parse notes within grace note group
        while (index < input.Length && input[index] != '}')
        {
            // Skip whitespace
            while (index < input.Length && char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            if (index >= input.Length || input[index] == '}')
            {
                break;
            }

            // Parse individual grace note (similar to normal note but without duration)
            if (!TryParseGraceNotePitch(input, ref index, out var pitch))
            {
                index = startIndex;
                return false;
            }

            pitches.Add(pitch);
        }

        if (index >= input.Length || input[index] != '}')
        {
            index = startIndex;
            return false;
        }

        index++; // Skip '}'

        if (pitches.Count == 0)
        {
            index = startIndex;
            return false;
        }

        graceNote = new GraceNote(pitches, isAcciaccatura);
        return true;
    }

    private static bool TryParseGraceNotePitch(
        string input,
        ref int index,
        out Pitch pitch)
    {
        pitch = default;

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

        // Parse note letter (A-G, a-g)
        if (index >= input.Length)
        {
            index = startIndex;
            return false;
        }

        char noteChar = input[index];
        if (!char.IsLetter(noteChar))
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
            _ => (PitchClass?)null
        };

        if (pitchClass == null)
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

        pitch = new Pitch(pitchClass.Value, octave, accidental);
        return true;
    }
}
