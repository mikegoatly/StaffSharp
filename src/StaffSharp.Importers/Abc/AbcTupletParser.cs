namespace StaffSharp.Importers.Abc;

using System.Diagnostics.CodeAnalysis;
using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Handles ABC tuplet notation (2, (3, (3:2, etc.).
/// </summary>
internal static class AbcTupletParser
{
    /// <summary>
    /// Tries to parse a tuplet specifier at the current position.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="index">Current position (will be advanced if tuplet found)</param>
    /// <param name="tuplet">The parsed tuplet</param>
    /// <param name="noteCount">Number of notes in this tuplet group</param>
    /// <returns>True if a tuplet specifier was found</returns>
    public static bool TryParseTuplet(
        string input,
        ref int index,
        [NotNullWhen(true)] out Tuplet? tuplet,
        out int noteCount)
    {
        tuplet = null;
        noteCount = 0;

        if (index >= input.Length || input[index] != '(')
        {
            return false;
        }

        var startIndex = index;
        index++; // Skip '('

        // Parse N (number of notes)
        if (index >= input.Length || !char.IsDigit(input[index]))
        {
            index = startIndex;
            return false;
        }

        var digitStart = index;
        while (index < input.Length && char.IsDigit(input[index]))
        {
            index++;
        }

        if (!int.TryParse(input[digitStart..index], out var actualNotes))
        {
            index = startIndex;
            return false;
        }

        noteCount = actualNotes;

        // Check for explicit :M (normalNotes)
        int normalNotes;
        if (index < input.Length && input[index] == ':')
        {
            index++; // Skip ':'

            if (index >= input.Length || !char.IsDigit(input[index]))
            {
                index = startIndex;
                return false;
            }

            digitStart = index;
            while (index < input.Length && char.IsDigit(input[index]))
            {
                index++;
            }

            if (!int.TryParse(input[digitStart..index], out normalNotes))
            {
                index = startIndex;
                return false;
            }

            // Optional :L (default length) - we'll ignore this for now as it's rarely used
            if (index < input.Length && input[index] == ':')
            {
                index++; // Skip second ':'
                // Parse and discard length specifier
                while (index < input.Length && char.IsDigit(input[index]))
                {
                    index++;
                }
            }
        }
        else
        {
            // Use default normalNotes based on actualNotes
            normalNotes = GetDefaultNormalNotes(actualNotes);
        }

        tuplet = new Tuplet(actualNotes, normalNotes);
        return true;
    }

    private static int GetDefaultNormalNotes(int actualNotes)
    {
        // ABC standard defaults:
        // 2 notes -> 3 (duplet)
        // 3 notes -> 2 (triplet)
        // 4 notes -> 3 (quadruplet)
        // 5 notes -> 4 (quintuplet)
        // 6 notes -> 4 (sextuplet)
        // 7 notes -> 6 (septuplet)
        // 8 notes -> 6 (octuplet)
        // 9 notes -> 8 (nonuplet)
        return actualNotes switch
        {
            2 => 3,
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 4,
            7 => 6,
            8 => 6,
            9 => 8,
            _ => actualNotes - 1 // Fallback
        };
    }
}
