namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;
using StaffSharp.Notation;

/// <summary>
/// Determines which notes need accidentals and positions them to avoid collisions.
/// </summary>
public class AccidentalPlacementPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                // Track accidentals throughout the measure
                foreach (var measure in staff.Measures)
                {
                    ProcessMeasure(measure, staff, context);
                }
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, LayoutStaff staff, SvgContext context)
    {
        // Track which accidentals have been applied in this measure
        // Key: MIDI note number, Value: accidental type
        var measureAccidentals = new Dictionary<int, Accidental>();
        var keySignature = staff.CurrentKeySignature;

        foreach (var symbol in measure.Symbols)
        {
            switch (symbol)
            {
                case NoteLayoutSymbol noteSymbol:
                    ProcessNoteAccidental(noteSymbol, keySignature, measureAccidentals, context);
                    break;

                case ChordLayoutSymbol chordSymbol:
                    ProcessChordAccidentals(chordSymbol, keySignature, measureAccidentals, context);
                    break;
            }
        }
    }

    private static void ProcessNoteAccidental(
        NoteLayoutSymbol noteSymbol,
        KeySignature keySignature,
        Dictionary<int, Accidental> measureAccidentals,
        SvgContext context)
    {
        var pitch = noteSymbol.Note.Pitch;

        if (KeySignatureService.NeedsAccidental(pitch, keySignature, measureAccidentals))
        {
            var accidental = KeySignatureService.GetAccidental(pitch);
            noteSymbol.Accidental = accidental;
            noteSymbol.AccidentalX = -context.StaffSpace; // Position to the left of notehead
            noteSymbol.AccidentalY = noteSymbol.Y;

            // Update measure tracking
            var midiNote = (int)pitch.ToMidiNote().Value;
            measureAccidentals[midiNote] = accidental;
        }
    }

    private static void ProcessChordAccidentals(
        ChordLayoutSymbol chordSymbol,
        KeySignature keySignature,
        Dictionary<int, Accidental> measureAccidentals,
        SvgContext context)
    {
        var accidentalColumnOffset = 0.0;
        var pitches = chordSymbol.Chord.Pitches.OrderByDescending(p => (int)p.ToMidiNote().Value).ToList();
        var yPositions = chordSymbol.NoteheadYPositions.ToList();

        for (int i = 0; i < pitches.Count; i++)
        {
            var pitch = pitches[i];

            if (KeySignatureService.NeedsAccidental(pitch, keySignature, measureAccidentals))
            {
                var accidental = KeySignatureService.GetAccidental(pitch);
                chordSymbol.Accidentals.Add(accidental);

                // Position accidentals to the left, stagger if needed
                var xOffset = -context.StaffSpace - accidentalColumnOffset;
                chordSymbol.AccidentalXOffsets.Add(xOffset);
                chordSymbol.AccidentalYPositions.Add(yPositions[i]);

                chordSymbol.AccidentalShifts.Add(true);

                // Update measure tracking
                var midiNote = (int)pitch.ToMidiNote().Value;
                measureAccidentals[midiNote] = accidental;

                // Check if next accidental needs to be in a different column
                if (i < pitches.Count - 1 && Math.Abs(yPositions[i] - yPositions[i + 1]) < context.StaffSpace)
                {
                    accidentalColumnOffset += context.StaffSpace;
                }
            }
            else
            {
                chordSymbol.AccidentalShifts.Add(false);
            }
        }
    }

}
