namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;

/// <summary>
/// Shifts noteheads in chords to avoid collisions when notes are a second apart.
/// </summary>
internal class HeadShiftPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                foreach (var measure in staff.Measures)
                {
                    foreach (var symbol in measure.Symbols.OfType<ChordLayoutSymbol>())
                    {
                        ProcessChord(symbol, context);
                    }
                }
            }
        }
    }

    private static void ProcessChord(ChordLayoutSymbol chordSymbol, SvgContext context)
    {
        var pitches = chordSymbol.Chord.Pitches.OrderBy(p => (int)p.ToMidiNote().Value).ToList();

        if (pitches.Count < 2)
        {
            return; // No collision possible
        }

        // Check for seconds (adjacent notes) and shift noteheads
        // In traditional notation:
        // - For stem-up chords: shift upper note right
        // - For stem-down chords: shift lower note right

        var headShifts = new List<double>(new double[pitches.Count]);

        for (int i = 0; i < pitches.Count - 1; i++)
        {
            var midiNote1 = (int)pitches[i].ToMidiNote().Value;
            var midiNote2 = (int)pitches[i + 1].ToMidiNote().Value;

            // Check if notes are a second apart (1 or 2 semitones)
            var interval = midiNote2 - midiNote1;
            if (interval <= 2)
            {
                // Notes are close together, need to shift one
                if (chordSymbol.Stem.Up)
                {
                    // Shift upper note right
                    headShifts[i + 1] = context.StaffSpace * 0.6;
                }
                else
                {
                    // Shift lower note right
                    headShifts[i] = context.StaffSpace * 0.6;
                }
            }
        }

        // Store the shifts in the chord symbol for rendering
        chordSymbol.NoteheadXShifts.Clear();
        foreach (var shift in headShifts)
        {
            chordSymbol.NoteheadXShifts.Add(shift);
        }
    }
}
