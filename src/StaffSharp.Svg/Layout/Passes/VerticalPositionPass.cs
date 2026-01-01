namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Assigns vertical positions (Y coordinates) to all symbols based on their pitch.
/// </summary>
public class VerticalPositionPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            double currentY = system.Y;

            foreach (var staff in system.Staves)
            {
                // Position the staff
                staff.Y = currentY;
                staff.Height = 4 * context.StaffSpace; // 5 staff lines, 4 spaces between them

                // Staff baseline (middle line of a 5-line staff) - RELATIVE to staff origin
                var staffBaseline = 2 * context.StaffSpace;

                foreach (var measure in staff.Measures)
                {
                    foreach (var symbol in measure.Symbols)
                    {
                        CalculateSymbolVerticalPosition(symbol, staffBaseline, context);
                    }
                }

                // Move to next staff
                currentY += staff.Height + (2 * context.StaffSpace);
            }

            // Update system height
            system.Height = currentY - system.Y;
        }
    }

    private static void CalculateSymbolVerticalPosition(LayoutSymbol symbol, double staffBaseline, SvgContext context)
    {
        switch (symbol)
        {
            case NoteLayoutSymbol noteSymbol:
                {
                    var staffPosition = PitchToStaffPosition(noteSymbol.Note.Pitch, Clef.Treble); // TODO: track actual clef
                    symbol.Y = staffBaseline - (staffPosition * 0.5 * context.StaffSpace);
                    symbol.Height = context.StaffSpace; // Height of a note head

                    // Calculate ledger lines needed
                    if (staffPosition > 5)
                    {
                        symbol.LedgerLineCount = (staffPosition - 5 + 1) / 2;
                        symbol.LedgerLinesAbove = true;
                    }
                    else if (staffPosition < -5)
                    {
                        symbol.LedgerLineCount = (-5 - staffPosition + 1) / 2;
                        symbol.LedgerLinesAbove = false;
                    }
                    break;
                }

            case RestLayoutSymbol:
                // Center rests on the staff
                symbol.Y = staffBaseline;
                symbol.Height = 2 * context.StaffSpace;
                break;

            case ChordLayoutSymbol chordSymbol:
                {
                    // For chords, position based on the highest and lowest notes
                    var pitches = chordSymbol.Chord.Pitches;
                    if (pitches.Count > 0)
                    {
                        var positions = pitches.Select(p => PitchToStaffPosition(p, Clef.Treble)).OrderBy(x => x).ToList();
                        var lowestPosition = positions[0];
                        var highestPosition = positions[^1];

                        // Store individual notehead positions
                        foreach (var pos in positions)
                        {
                            var y = staffBaseline - (pos * 0.5 * context.StaffSpace);
                            chordSymbol.NoteheadYPositions.Add(y);
                        }

                        // Symbol Y is positioned at the bottom note
                        symbol.Y = staffBaseline - (lowestPosition * 0.5 * context.StaffSpace);
                        symbol.Height = Math.Abs(highestPosition - lowestPosition) * 0.5 * context.StaffSpace + context.StaffSpace;

                        // Calculate ledger lines
                        if (highestPosition > 5)
                        {
                            symbol.LedgerLineCount = (highestPosition - 5 + 1) / 2;
                            symbol.LedgerLinesAbove = true;
                        }
                        else if (lowestPosition < -5)
                        {
                            symbol.LedgerLineCount = (-5 - lowestPosition + 1) / 2;
                            symbol.LedgerLinesAbove = false;
                        }
                    }
                    break;
                }

            case ClefLayoutSymbol clefSymbol:
                // Position clef symbol (Y is relative to staff origin)
                // Treble clef: The spiral wraps around the G line (second line from bottom)
                // G line is at staff position +2 from baseline, which is Y = baseline - 1 staff space
                // Bass clef: The dots straddle the F line (fourth line from bottom, which is the baseline)
                if (clefSymbol.Clef == Clef.Treble)
                {
                    // Position so the clef is centered vertically on the staff
                    // Treble clef's defining point (where the spiral curl centers) should be at the G line
                    // G line (second from bottom) is at Y = 30 (staff line index 3 * 10)
                    symbol.Y = 3 * context.StaffSpace; // Position at G line
                }
                else if (clefSymbol.Clef == Clef.Bass)
                {
                    // Bass clef dots should straddle the F line (second from top = baseline)
                    symbol.Y = 2 * context.StaffSpace; // Position at middle line (F)
                }
                else
                {
                    symbol.Y = 2 * context.StaffSpace; // Default to middle line
                }
                symbol.Height = 4 * context.StaffSpace;
                break;

            case KeySignatureLayoutSymbol:
                // Position at top of staff (Y coordinates are relative to staff)
                symbol.Y = 0;
                symbol.Height = 4 * context.StaffSpace;
                break;

            case TimeSignatureLayoutSymbol:
                // Position at top of staff
                symbol.Y = 0;
                symbol.Height = 4 * context.StaffSpace;
                break;

            case BarlineLayoutSymbol:
                // Position at top of staff
                symbol.Y = 0;
                symbol.Height = 4 * context.StaffSpace;
                break;
        }
    }

    /// <summary>
    /// Converts a pitch to a staff position.
    /// Staff position 0 = middle line (B4 in treble clef).
    /// Positive values are above, negative below.
    /// </summary>
    private static int PitchToStaffPosition(Pitch pitch, Clef clef)
    {
        // For treble clef: B4 (MIDI 71) is on the middle line (position 0)
        // Each semitone that's a natural note changes position by 1
        // C4 = MIDI 60, B4 = MIDI 71

        if (clef == Clef.Treble)
        {
            // Map MIDI note number to staff position
            // Middle line (B4) = MIDI 71 = position 0
            // The staff positions for natural notes in treble clef:
            // C5=72->2, D5=74->3, E5=76->4, F5=77->5, G5=79->6, A5=81->7, B5=83->8, C6=84->9
            // C4=60->-6, D4=62->-5, E4=64->-4, F4=65->-3, G4=67->-2, A4=69->-1, B4=71->0

            var midiNote = (int)pitch.ToMidiNote().Value;
            var octave = (midiNote / 12) - 1; // MIDI octave (C4 = octave 4)
            var noteClass = midiNote % 12;

            // Map note class to position within octave (C=0, D=1, E=2, F=3, G=4, A=5, B=6)
            var diatonicPosition = noteClass switch
            {
                0 => 0,  // C
                2 => 1,  // D
                4 => 2,  // E
                5 => 3,  // F
                7 => 4,  // G
                9 => 5,  // A
                11 => 6, // B
                _ => noteClass / 2  // Approximate for accidentals
            };

            // Calculate staff position relative to B4 (middle line)
            // B4 is octave 4, position 6 within octave
            var positionFromC0 = (octave * 7) + diatonicPosition;
            var b4PositionFromC0 = (4 * 7) + 6; // B4

            return positionFromC0 - b4PositionFromC0;
        }

        // TODO: Handle other clefs
        return 0;
    }
}
