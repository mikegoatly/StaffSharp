namespace StaffSharp.Layout.Services;

using StaffSharp.Notation;

/// <summary>
/// Service for calculating staff positions from pitches for different clef types.
/// </summary>
internal static class PitchCalculator
{
    /// <summary>
    /// Converts a pitch to a staff position for the given clef.
    /// Staff position 0 = middle line of a 5-line staff.
    /// Positive values are above the middle line, negative below.
    /// </summary>
    /// <param name="pitch">The pitch to convert</param>
    /// <param name="clef">The clef context</param>
    /// <returns>The staff position (0 = middle line, +1 = one line/space above, -1 = one line/space below)</returns>
    public static int PitchToStaffPosition(Pitch pitch, Clef clef)
    {
        var midiNote = (int)pitch.ToMidiNote().Value;
        var middleLineMidiNote = GetMiddleLineMidiNote(clef);

        // Calculate octave from MIDI note
        var octave = (midiNote / 12) - 1; // MIDI octave (C4 = octave 4)

        // Map PitchClass enum directly to diatonic step (0-6)
        // This preserves the original letter name from the notation source (e.g., Db vs C#)
        var diatonicStep = pitch.PitchClass switch
        {
            PitchClass.C => 0,
            PitchClass.CSharp => 0,    // C# positioned as C
            PitchClass.D => 1,
            PitchClass.DSharp => 1,    // D# positioned as D
            PitchClass.E => 2,
            PitchClass.F => 3,
            PitchClass.FSharp => 3,    // F# positioned as F
            PitchClass.G => 4,
            PitchClass.GSharp => 4,    // G# positioned as G
            PitchClass.A => 5,
            PitchClass.ASharp => 5,    // A# positioned as A
            PitchClass.B => 6,
            _ => 0  // Fallback for unexpected pitch classes
        };

        // Calculate diatonic position from C0
        var positionFromC0 = (octave * 7) + diatonicStep;

        // Calculate middle line diatonic position
        var middleLineOctave = (middleLineMidiNote / 12) - 1;
        var middleLineNoteClass = middleLineMidiNote % 12;
        var middleLineDiatonicStep = middleLineNoteClass switch
        {
            0 => 0,   // C
            2 => 1,   // D
            4 => 2,   // E
            5 => 3,   // F
            7 => 4,   // G
            9 => 5,   // A
            11 => 6,  // B
            _ => middleLineNoteClass / 2  // Approximate (shouldn't happen for standard clefs)
        };
        var middleLinePositionFromC0 = (middleLineOctave * 7) + middleLineDiatonicStep;

        // Return relative position to middle line
        return positionFromC0 - middleLinePositionFromC0;
    }

    /// <summary>
    /// Gets the MIDI note number that appears on the middle line for each clef type.
    /// </summary>
    /// <param name="clef">The clef</param>
    /// <returns>MIDI note number (e.g., 71 for B4 in treble clef)</returns>
    public static int GetMiddleLineMidiNote(Clef clef)
    {
        return clef switch
        {
            Clef.Treble => 71,  // B4
            Clef.Bass => 50,    // D3
            Clef.Alto => 60,    // C4
            Clef.Tenor => 57,   // A3
            _ => 71  // Default to treble
        };
    }
}
