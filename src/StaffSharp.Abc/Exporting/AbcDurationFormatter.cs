namespace StaffSharp.Abc.Exporting;

using System.Globalization;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Formats symbolic durations as ABC notation duration modifiers.
/// </summary>
internal static class AbcDurationFormatter
{
    /// <summary>
    /// Formats a symbolic duration as ABC notation relative to the default note length.
    /// </summary>
    /// <param name="duration">The symbolic duration to format.</param>
    /// <param name="defaultNoteLength">The default note length (L: header value, e.g., 1/8 = eighth note).</param>
    /// <returns>
    /// The ABC duration modifier:
    /// - "" (empty) if duration equals default
    /// - "2", "3", "4", etc. for multiples
    /// - "/" for half (shorthand for /2)
    /// - "/2", "/4", etc. for divisions
    /// </returns>
    /// <remarks>
    /// Reverses the logic in AbcEventParser.ParseDuration (line 357-398).
    ///
    /// The defaultNoteLength is in "note duration" form (1/8, 1/4, etc.), but needs to be
    /// converted to beats for comparison. The import code does: beats = (numerator * 4) / denominator.
    /// For example: L:1/8 → (1 * 4) / 8 = 1/2 beat.
    ///
    /// IMPORTANT: When formatting durations for tuplet notes, we must use the BASE duration
    /// (without the tuplet effect), since the tuplet is specified separately with (3, (5, etc.
    /// </remarks>
    public static string Format(SymbolicDuration duration, Rational defaultNoteLength)
    {
        // Convert defaultNoteLength from "note duration" to beats (same as import logic)
        // Example: 1/8 note → (1 * 4) / 8 = 1/2 beat
        var defaultBeats = Rational.Create(defaultNoteLength.Numerator * 4, defaultNoteLength.Denominator);

        // Get the BASE duration in beats (without tuplet effect)
        // If the duration has a tuplet, we need to "un-apply" it for ABC notation
        var durationBeats = GetBaseDurationBeats(duration);

        // Calculate multiplier: how many times the default length?
        var multiplier = durationBeats / defaultBeats;

        // If it's exactly the default length, omit the duration modifier
        if (multiplier.Numerator == multiplier.Denominator)
        {
            return string.Empty;
        }

        // If it's a whole number multiple (2x, 3x, 4x, etc.)
        if (multiplier.Denominator == 1)
        {
            return multiplier.Numerator.ToString(CultureInfo.InvariantCulture);
        }

        // If it's a simple division (1/2, 1/4, etc.)
        if (multiplier.Numerator == 1)
        {
            // Use shorthand "/" for /2
            if (multiplier.Denominator == 2)
            {
                return "/";
            }

            return $"/{multiplier.Denominator.ToString(CultureInfo.InvariantCulture)}";
        }

        // For complex fractions like 3/2 (dotted notes relative to default)
        // ABC supports this as "3/2" notation
        return $"{multiplier.Numerator.ToString(CultureInfo.InvariantCulture)}/{multiplier.Denominator.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Gets the base duration in beats without the tuplet effect.
    /// </summary>
    /// <remarks>
    /// In ABC notation, tuplets are specified separately (e.g., (3CDE), so the duration
    /// modifier on each note should be the base duration, not the tuplet-adjusted duration.
    ///
    /// For example, `(3CDE` with `L:1/8`:
    /// - Each note has base duration = Eighth (1/8 note = 1/2 beat)
    /// - Tuplet (3,2) makes it 1/2 * 2/3 = 1/3 beat
    /// - But when exporting, we write `C` (not `C/3`) because the `(3` handles the tuplet
    /// </remarks>
    private static Rational GetBaseDurationBeats(SymbolicDuration duration)
    {
        // Base duration in beats (quarter note = 1 beat)
        var baseBeats = Rational.Create(4, (int)duration.Base);

        // Apply dots (each dot adds half the previous value)
        if (duration.Dots > 0)
        {
            var multiplier = Rational.Create(1, 1);
            var dotValue = Rational.Create(1, 2);

            for (int i = 0; i < duration.Dots; i++)
            {
                multiplier += dotValue;
                dotValue *= Rational.Create(1, 2);
            }

            baseBeats *= multiplier;
        }

        // Do NOT apply tuplet - that's the key difference from ToBeats()
        // The tuplet is written separately in ABC notation

        return baseBeats;
    }
}
