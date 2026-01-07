namespace StaffSharp.Abc.Importing;

using System.Diagnostics.CodeAnalysis;

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
            // Grace notes don't support quarter tones or rests
            if (!AbcEventParser.TryParsePitch(input, ref index, allowQuarterTones: false, allowRests: false, out var pitch) || !pitch.HasValue)
            {
                index = startIndex;
                return false;
            }

            pitches.Add(pitch.Value);
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
}
