namespace StaffSharp.Svg.Layout.Services;

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

        // Calculate octave and diatonic position
        var octave = (midiNote / 12) - 1; // MIDI octave (C4 = octave 4)
        var noteClass = midiNote % 12;

        // Map note class to diatonic position within octave (C=0, D=1, E=2, F=3, G=4, A=5, B=6)
        var diatonicPosition = noteClass switch
        {
            0 => 0,  // C
            2 => 1,  // D
            4 => 2,  // E
            5 => 3,  // F
            7 => 4,  // G
            9 => 5,  // A
            11 => 6, // B
            _ => noteClass / 2  // Approximate for accidentals (C#=0.5≈0, D#=1.5≈1, etc.)
        };

        // Calculate position from C0
        var positionFromC0 = (octave * 7) + diatonicPosition;

        // Calculate middle line position from C0
        var middleLineOctave = (middleLineMidiNote / 12) - 1;
        var middleLineNoteClass = middleLineMidiNote % 12;
        var middleLineDiatonicPosition = middleLineNoteClass switch
        {
            0 => 0,  // C
            2 => 1,  // D
            4 => 2,  // E
            5 => 3,  // F
            7 => 4,  // G
            9 => 5,  // A
            11 => 6, // B
            _ => middleLineNoteClass / 2
        };
        var middleLinePositionFromC0 = (middleLineOctave * 7) + middleLineDiatonicPosition;

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
