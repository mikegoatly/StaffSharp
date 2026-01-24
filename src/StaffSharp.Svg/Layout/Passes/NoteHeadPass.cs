namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;

/// <summary>
/// Calculates bounds for noteheads of all stemmed symbols, and position noteheads in chords to 
/// avoid collisions when notes are a second apart.
/// </summary>
internal class NoteHeadPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        foreach (var staff in model.Systems.SelectMany(s => s.Staves))
        {
            foreach (var symbol in staff.Measures.SelectMany(m => m.Symbols.OfType<IStemmedSymbol>()))
            {
                // Set note head bounds
                var noteWidth = context.GetNoteheadWidth(symbol.Duration.Base);

                if (symbol is ChordLayoutSymbol chordSymbol)
                {
                    var hasHeadShifts = ProcessChord(chordSymbol, staff, context);

                    var minY = chordSymbol.NoteheadYPositions.Min();
                    var maxY = chordSymbol.NoteheadYPositions.Max();
                    symbol.NoteHeadBounds = new Bounds(
                        symbol.Bounds.X,
                        minY - context.HalfStaffSpace,
                        noteWidth + (hasHeadShifts ? noteWidth * 0.5 : 0D),
                        maxY - minY + context.StaffSpace
                    );
                }
                else if (symbol is NoteLayoutSymbol note)
                {
                    symbol.NoteHeadBounds = new Bounds(
                        symbol.Bounds.X,
                        symbol.Bounds.Y - context.HalfStaffSpace,
                        noteWidth,
                        context.StaffSpace
                    );
                }
            }
        }
    }

    private static bool ProcessChord(ChordLayoutSymbol chordSymbol, LayoutStaff staff, SvgContext context)
    {
        if (chordSymbol.NoteheadYPositions.Count < 2)
        {
            return false; // No collision possible
        }

        // Get the actual notehead width for this chord's duration
        var noteheadWidth = context.GetNoteheadWidth(chordSymbol.Chord.Duration.Base);
        var shiftDistance = noteheadWidth * 0.55; // Slight overlap for aesthetics

        // Create a mapping of Y positions to pitches to check intervals
        // NoteheadYPositions is sorted by staff position (ascending)
        // We need to match each Y position with its corresponding pitch
        var pitchesWithPositions = chordSymbol.Chord.Pitches
            .Select(p => new { Pitch = p, StaffPos = PitchCalculator.PitchToStaffPosition(p, staff.CurrentClef) })
            .OrderBy(x => x.StaffPos)
            .ToList();

        // Initialize shifts array matching NoteheadYPositions ordering
        var headShifts = new double[chordSymbol.NoteheadYPositions.Count];

        // Check for seconds (adjacent notes on staff) and shift noteheads
        // In traditional notation:
        // - For stem-up chords: shift upper note right (higher staff position = later in array)
        // - For stem-down chords: shift lower note right (lower staff position = earlier in array)
        var hasHeadShifts = false;
        for (int i = 0; i < pitchesWithPositions.Count - 1; i++)
        {
            var pitch1 = pitchesWithPositions[i].Pitch;
            var pitch2 = pitchesWithPositions[i + 1].Pitch;

            var midiNote1 = (int)pitch1.ToMidiNote().Value;
            var midiNote2 = (int)pitch2.ToMidiNote().Value;

            // Check if notes are a second apart (1 or 2 semitones)
            var interval = midiNote2 - midiNote1;
            if (interval <= 2)
            {
                // Notes are close together, need to shift one
                if (chordSymbol.Stem.Up)
                {
                    // Shift upper note right (later in array = higher staff position)
                    headShifts[i + 1] = shiftDistance;
                }
                else
                {
                    // Shift lower note right (earlier in array = lower staff position)
                    headShifts[i] = shiftDistance;
                }

                hasHeadShifts = true;
            }
        }

        // Store the shifts in the chord symbol for rendering
        chordSymbol.NoteheadXShifts.Clear();
        chordSymbol.NoteheadXShifts.AddRange(headShifts);

        return hasHeadShifts;
    }
}
