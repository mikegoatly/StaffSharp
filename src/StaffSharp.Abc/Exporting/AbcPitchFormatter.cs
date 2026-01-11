namespace StaffSharp.Abc.Exporting;

using System.Text;

using StaffSharp.Notation;

/// <summary>
/// Formats pitches as ABC notation.
/// </summary>
internal static class AbcPitchFormatter
{
    /// <summary>
    /// Formats a pitch as ABC notation.
    /// </summary>
    /// <param name="pitch">The pitch to format.</param>
    /// <returns>
    /// The ABC pitch notation:
    /// - Accidentals: ^, ^^, _, __, =, ^/, _/, ^3/, _3/
    /// - Note letter: A-G (uppercase for octave 3-4, lowercase for octave 5+)
    /// - Octave modifiers: , (lower) or ' (higher)
    /// </returns>
    /// <remarks>
    /// Reverses the logic in AbcEventParser.TryParsePitch (line 225-355).
    /// Default octaves: uppercase letters = octave 4, lowercase letters = octave 5.
    /// </remarks>
    public static string Format(Pitch pitch)
    {
        var result = new StringBuilder();

        // Step 1: Accidental (if present)
        if (pitch.Accidental.HasValue)
        {
            result.Append(pitch.Accidental.Value switch
            {
                Accidental.DoubleFlat => "__",
                Accidental.Flat => "_",
                Accidental.Natural => "=",
                Accidental.Sharp => "^",
                Accidental.DoubleSharp => "^^",
                Accidental.QuarterFlat => "_/",
                Accidental.QuarterSharp => "^/",
                Accidental.ThreeQuarterFlat => "_3/",
                Accidental.ThreeQuarterSharp => "^3/",
                _ => ""
            });
        }

        // Step 2: Note letter (with correct case based on octave)
        // Uppercase = octaves 3-4, Lowercase = octaves 5+
        char noteLetter = pitch.PitchClass switch
        {
            PitchClass.C => 'C',
            PitchClass.D => 'D',
            PitchClass.E => 'E',
            PitchClass.F => 'F',
            PitchClass.G => 'G',
            PitchClass.A => 'A',
            PitchClass.B => 'B',
            _ => 'C'
        };

        // Use lowercase for octave 5 and above
        if (pitch.Octave >= 5)
        {
            noteLetter = char.ToLowerInvariant(noteLetter);
        }

        result.Append(noteLetter);

        // Step 3: Octave modifiers
        // Octave 4 with uppercase = no modifier (default)
        // Octave 5 with lowercase = no modifier (default)
        // Below defaults: add commas
        // Above defaults: add apostrophes

        if (pitch.Octave < 4)
        {
            // Below octave 4: add commas
            result.Append(new string(',', 4 - pitch.Octave));
        }
        else if (pitch.Octave > 5)
        {
            // Above octave 5: add apostrophes
            result.Append(new string('\'', pitch.Octave - 5));
        }
        // Octaves 4-5 need no modifiers (already handled by letter case)

        return result.ToString();
    }
}
