namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;

using StaffSharp.Layout.Services;

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

                // Staff baseline (middle line of a 5-line staff) - RELATIVE to staff origin
                var staffBaseline = 2 * context.StaffSpace;

                foreach (var measure in staff.Measures)
                {
                    foreach (var symbol in measure.Symbols)
                    {
                        CalculateSymbolVerticalPosition(symbol, staff, staffBaseline, context);
                    }
                }

                // Move to next staff (temporary height, will be recalculated by BoundsCalculationPass)
                var tempHeight = 4 * context.StaffSpace; // 5 staff lines = 4 spaces
                currentY += tempHeight + (2 * context.StaffSpace);
            }

            // Update system height
            system.Height = currentY - system.Y;
        }
    }

    private static void CalculateSymbolVerticalPosition(LayoutSymbol symbol, LayoutStaff staff, double staffBaseline, SvgContext context)
    {
        switch (symbol)
        {
            case NoteLayoutSymbol noteSymbol:
                {
                    var staffPosition = PitchCalculator.PitchToStaffPosition(noteSymbol.Note.Pitch, staff.CurrentClef);
                    symbol.Y = staffBaseline - (staffPosition * 0.5 * context.StaffSpace);

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
                break;

            case ChordLayoutSymbol chordSymbol:
                {
                    // For chords, position based on the highest and lowest notes
                    var pitches = chordSymbol.Chord.Pitches;
                    if (pitches.Count > 0)
                    {
                        var positions = pitches.Select(p => PitchCalculator.PitchToStaffPosition(p, staff.CurrentClef)).OrderBy(x => x).ToList();
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
                symbol.Y = ClefCalculator.GetClefYPosition(clefSymbol.Clef, context);
                break;

            case KeySignatureLayoutSymbol:
                // Position at top of staff (Y coordinates are relative to staff)
                symbol.Y = 0;
                break;

            case TimeSignatureLayoutSymbol:
                // Position at top of staff
                symbol.Y = 0;
                break;

            case BarlineLayoutSymbol:
                // Position at top of staff, spanning full staff height
                symbol.Y = 0;
                symbol.Height = 4 * context.StaffSpace;  // 5 lines = 4 spaces
                break;
        }
    }

}
