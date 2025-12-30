namespace StaffSharp.Importers.Abc;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Handles ABC broken rhythm notation (>, <, >>, <<<, etc.).
/// </summary>
internal static class AbcBrokenRhythmParser
{
    /// <summary>
    /// Tries to parse a broken rhythm operator and returns the multipliers.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="index">Current position (will be advanced if operator found)</param>
    /// <param name="firstNoteMultiplier">Multiplier for the first note</param>
    /// <param name="secondNoteMultiplier">Multiplier for the second note</param>
    /// <returns>True if a broken rhythm operator was found</returns>
    public static bool TryParseBrokenRhythm(
        string input,
        ref int index,
        out Rational firstNoteMultiplier,
        out Rational secondNoteMultiplier)
    {
        firstNoteMultiplier = Rational.Create(1, 1);
        secondNoteMultiplier = Rational.Create(1, 1);

        if (index >= input.Length)
        {
            return false;
        }

        var brokenRhythmChar = input[index];
        if (brokenRhythmChar != '>' && brokenRhythmChar != '<')
        {
            return false;
        }

        // Count consecutive broken rhythm symbols
        var count = 0;
        while (index < input.Length && input[index] == brokenRhythmChar)
        {
            count++;
            index++;
        }

        // Calculate multipliers based on count
        // For n symbols:
        // - First note: (2^(n+1) - 1) / 2^n
        // - Second note: 1 / 2^n

        var powerOfTwo = 1 << count; // 2^count
        var numeratorFirst = (1 << (count + 1)) - 1; // 2^(count+1) - 1

        if (brokenRhythmChar == '>')
        {
            // A>B: A gets longer, B gets shorter
            firstNoteMultiplier = Rational.Create(numeratorFirst, powerOfTwo);
            secondNoteMultiplier = Rational.Create(1, powerOfTwo);
        }
        else // '<'
        {
            // A<B: A gets shorter, B gets longer
            firstNoteMultiplier = Rational.Create(1, powerOfTwo);
            secondNoteMultiplier = Rational.Create(numeratorFirst, powerOfTwo);
        }

        return true;
    }
}
