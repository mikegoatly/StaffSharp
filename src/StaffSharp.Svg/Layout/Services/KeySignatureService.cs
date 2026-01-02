namespace StaffSharp.Svg.Layout.Services;

using StaffSharp.Notation;

/// <summary>
/// Service for handling key signature logic including affected pitches, accidental display rules, and rendering positions.
/// </summary>
internal static class KeySignatureService
{
    private static readonly int[] sharpPitchClasses = [6, 1, 8, 3, 10, 5, 0]; // F C G D A E B
    private static readonly int[] flatPitchClasses = [10, 3, 8, 1, 6, 11, 4]; // B E A D G C F

    /// <summary>
    /// Gets the set of pitch classes (0-11) affected by the key signature and their accidentals.
    /// For example, G major returns { 6 => Sharp } (F# is pitch class 6).
    /// </summary>
    public static IReadOnlyDictionary<int, Accidental> GetAffectedPitches(KeySignature keySignature)
    {
        var affected = new Dictionary<int, Accidental>();

        // Map key signatures to their sharps or flats
        // Order of sharps: F C G D A E B (pitch classes: 5, 0, 7, 2, 9, 4, 11)
        // Order of flats: B E A D G C F (pitch classes: 10, 3, 8, 1, 6, 11, 4)

        var sharpCount = keySignature.Sharps;
        if (sharpCount > 0)
        {
            for (int i = 0; i < sharpCount && i < sharpPitchClasses.Length; i++)
            {
                affected[sharpPitchClasses[i]] = Accidental.Sharp;
            }
        }
        else if (sharpCount < 0)
        {
            // Negative sharps means flats
            var flatCount = -sharpCount;
            for (int i = 0; i < flatCount && i < flatPitchClasses.Length; i++)
            {
                affected[flatPitchClasses[i]] = Accidental.Flat;
            }
        }

        return affected;
    }

    /// <summary>
    /// Determines if a note needs an accidental symbol displayed, considering the key signature and measure context.
    /// </summary>
    public static bool NeedsAccidental(
        Pitch pitch,
        KeySignature keySignature,
        Dictionary<int, Accidental> measureAccidentals)
    {
        var midiNote = (int)pitch.ToMidiNote().Value;
        var pitchClass = midiNote % 12;
        var accidental = GetAccidental(pitch);

        // Get key signature affected pitches
        var keyAccidentals = GetAffectedPitches(keySignature);

        // Check if this pitch class is in the key signature
        var isInKeySignature = keyAccidentals.TryGetValue(pitchClass, out var keyAccidental);

        // If the note matches the key signature, don't show accidental
        if (isInKeySignature && accidental == keyAccidental)
        {
            return false;
        }

        // If the note is natural and not altered by key signature, don't show accidental
        if (accidental == Accidental.Natural && !isInKeySignature)
        {
            // Unless a previous accidental in the measure affects this note
            if (measureAccidentals.TryGetValue(midiNote, out var previousAccidental))
            {
                // Show natural to cancel previous accidental
                return previousAccidental != Accidental.Natural;
            }
            return false;
        }

        // If key signature has an accidental but this note is natural, show natural sign
        if (isInKeySignature && accidental == Accidental.Natural)
        {
            return true;
        }

        // Check measure context
        if (measureAccidentals.TryGetValue(midiNote, out var prevAccidental))
        {
            // If same accidental as previous in measure, don't repeat
            return accidental != prevAccidental;
        }

        // Show accidental (sharp or flat)
        return accidental != Accidental.Natural;
    }

    /// <summary>
    /// Gets the accidental type for a pitch based on its MIDI note.
    /// </summary>
    public static Accidental GetAccidental(Pitch pitch)
    {
        var noteClass = (int)pitch.ToMidiNote().Value % 12;

        return noteClass switch
        {
            1 => Accidental.Sharp,    // C#/Db
            3 => Accidental.Sharp,    // D#/Eb
            6 => Accidental.Sharp,    // F#/Gb
            8 => Accidental.Sharp,    // G#/Ab
            10 => Accidental.Sharp,   // A#/Bb
            _ => Accidental.Natural
        };
    }

    /// <summary>
    /// Calculates the horizontal width required to render a key signature.
    /// Standard spacing between accidentals in key signatures.
    /// </summary>
    public const double AccidentalSpacing = 0.7;

    /// <summary>
    /// Calculates the horizontal width required to render a key signature.
    /// </summary>
    public static double CalculateWidth(KeySignature keySignature, double staffSpace)
    {
        if (keySignature == KeySignature.C)
        {
            return 0;
        }

        var accidentalCount = Math.Abs(keySignature.Sharps);
        // Each accidental takes AccidentalSpacing staff spaces
        // Last accidental doesn't need trailing space, so (n-1) spacings
        return (accidentalCount - 1) * AccidentalSpacing * staffSpace + (1.0 * staffSpace);
    }

    /// <summary>
    /// Gets the Y positions (relative to staff origin) for each accidental in the key signature.
    /// Positions vary by clef type.
    /// </summary>
    public static IReadOnlyList<(Accidental Accidental, double YPosition)> GetAccidentalPositions(
        KeySignature keySignature,
        Clef clef,
        double staffSpace)
    {
        var positions = new List<(Accidental, double)>();

        if (keySignature == KeySignature.C)
        {
            return positions;
        }

        var sharpCount = keySignature.Sharps;
        if (sharpCount > 0)
        {
            // Sharps: F C G D A E B
            var sharpPositions = GetSharpPositions(clef, staffSpace);
            for (int i = 0; i < sharpCount && i < sharpPositions.Count; i++)
            {
                positions.Add((Accidental.Sharp, sharpPositions[i]));
            }
        }
        else
        {
            // Flats: B E A D G C F
            var flatCount = -sharpCount;
            var flatPositions = GetFlatPositions(clef, staffSpace);
            for (int i = 0; i < flatCount && i < flatPositions.Count; i++)
            {
                positions.Add((Accidental.Flat, flatPositions[i]));
            }
        }

        return positions;
    }

    private static List<double> GetSharpPositions(Clef clef, double staffSpace)
    {
        // Positions for sharps (F C G D A E B) relative to staff origin (Y=0 at top line)
        // These are the Y positions where each sharp should be drawn
        return clef switch
        {
            Clef.Treble =>
            [
                0.0 * staffSpace,  // F# - Top line
                1.5 * staffSpace,  // C# - 3rd space
                -0.5 * staffSpace, // G# - Space above staff
                1.0 * staffSpace,  // D# - 4th line
                2.5 * staffSpace,  // A# - 2nd space
                0.5 * staffSpace,  // E# - 4th space
                2.0 * staffSpace   // B# - 3rd line
            ],
            Clef.Bass =>
            [
                1.0 * staffSpace,  // F# - 4th line (from bottom, so 2nd from top)
                2.5 * staffSpace,  // C# - 2nd space
                0.5 * staffSpace,  // G# - 4th space
                2.0 * staffSpace,  // D# - 3rd line
                3.5 * staffSpace,  // A# - 1st space
                1.5 * staffSpace,  // E# - 3rd space
                3.0 * staffSpace   // B# - 2nd line
            ],
            Clef.Alto =>
            [
                1.0 * staffSpace,  // F# - 4th line
                3.0 * staffSpace,  // C# - 2nd line
                -0.5 * staffSpace, // G# - Space above staff
                2.0 * staffSpace,  // D# - 3rd line (center)
                4.0 * staffSpace,  // A# - 1st line (bottom)
                2.5 * staffSpace,  // E# - 2nd space
                0.5 * staffSpace   // B# - 4th space
            ],
            Clef.Tenor =>
            [
                0.0 * staffSpace,  // F# - 5th line (top)
                2.0 * staffSpace,  // C# - 3rd line (center)
                -0.5 * staffSpace, // G# - Space above staff
                1.0 * staffSpace,  // D# - 4th line
                3.0 * staffSpace,  // A# - 2nd line
                1.5 * staffSpace,  // E# - 3rd space
                3.5 * staffSpace   // B# - 1st space
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(clef), clef, null)
        };
    }

    private static List<double> GetFlatPositions(Clef clef, double staffSpace)
    {
        // Positions for flats (B E A D G C F) relative to staff origin
        return clef switch
        {
            Clef.Treble =>
            [
                2.0 * staffSpace,  // Bb - 3rd line (middle)
                0.5 * staffSpace,  // Eb - 4th space
                2.5 * staffSpace,  // Ab - 2nd space
                1.0 * staffSpace,  // Db - 4th line
                3.0 * staffSpace,  // Gb - 2nd line
                1.5 * staffSpace,  // Cb - 3rd space
                3.5 * staffSpace   // Fb - 1st space
            ],
            Clef.Bass =>
            [
                3.0 * staffSpace,  // Bb - 2nd line
                1.5 * staffSpace,  // Eb - 3rd space
                3.5 * staffSpace,  // Ab - 1st space
                2.0 * staffSpace,  // Db - 3rd line
                4.0 * staffSpace,  // Gb - 1st line (bottom)
                2.5 * staffSpace,  // Cb - 2nd space
                4.5 * staffSpace   // Fb - Space below staff
            ],
            Clef.Alto =>
            [
                2.5 * staffSpace,  // Bb - 3rd space
                1.0 * staffSpace,  // Eb - 4th line
                3.0 * staffSpace,  // Ab - 2nd line
                1.5 * staffSpace,  // Db - 3rd space (above center)
                3.5 * staffSpace,  // Gb - 1st space
                2.0 * staffSpace,  // Cb - 3rd line (center)
                4.0 * staffSpace   // Fb - 1st line (bottom)
            ],
            Clef.Tenor =>
            [
                3.5 * staffSpace,  // Bb - 1st space
                2.0 * staffSpace,  // Eb - 3rd line (center)
                4.0 * staffSpace,  // Ab - 1st line (bottom)
                2.5 * staffSpace,  // Db - 2nd space
                4.5 * staffSpace,  // Gb - Space below staff
                3.0 * staffSpace,  // Cb - 2nd line
                5.0 * staffSpace   // Fb - Below staff (ledger line area)
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(clef), clef, null)
        };
    }
}
