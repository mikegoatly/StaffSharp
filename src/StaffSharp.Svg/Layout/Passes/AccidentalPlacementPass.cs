namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

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
                    ProcessMeasure(measure, context);
                }
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, SvgContext context)
    {
        // Track which accidentals have been applied in this measure
        // Key: MIDI note number, Value: accidental type
        var measureAccidentals = new Dictionary<int, Accidental>();

        // For MVP: assume C major (no key signature)
        // TODO: Get actual key signature from context

        foreach (var symbol in measure.Symbols)
        {
            switch (symbol)
            {
                case NoteLayoutSymbol noteSymbol:
                    ProcessNoteAccidental(noteSymbol, measureAccidentals, context);
                    break;

                case ChordLayoutSymbol chordSymbol:
                    ProcessChordAccidentals(chordSymbol, measureAccidentals, context);
                    break;
            }
        }
    }

    private static void ProcessNoteAccidental(NoteLayoutSymbol noteSymbol, Dictionary<int, Accidental> measureAccidentals, SvgContext context)
    {
        var pitch = noteSymbol.Note.Pitch;
        var midiNote = (int)pitch.ToMidiNote().Value;
        var accidental = GetAccidental(pitch);

        // Check if we need to display an accidental
        if (NeedsAccidental(midiNote, accidental, measureAccidentals))
        {
            noteSymbol.Accidental = accidental;
            noteSymbol.AccidentalX = -1.5 * context.StaffSpace; // Position to the left of notehead
            noteSymbol.AccidentalY = noteSymbol.Y;
            measureAccidentals[midiNote] = accidental;
        }
        else if (measureAccidentals.ContainsKey(midiNote))
        {
            // Note matches a previous accidental in the measure
            measureAccidentals[midiNote] = accidental;
        }
    }

    private static void ProcessChordAccidentals(ChordLayoutSymbol chordSymbol, Dictionary<int, Accidental> measureAccidentals, SvgContext context)
    {
        var accidentalColumnOffset = 0.0;
        var pitches = chordSymbol.Chord.Pitches.OrderByDescending(p => (int)p.ToMidiNote().Value).ToList();
        var yPositions = chordSymbol.NoteheadYPositions.ToList();

        for (int i = 0; i < pitches.Count; i++)
        {
            var pitch = pitches[i];
            var midiNote = (int)pitch.ToMidiNote().Value;
            var accidental = GetAccidental(pitch);

            if (NeedsAccidental(midiNote, accidental, measureAccidentals))
            {
                chordSymbol.Accidentals.Add(accidental);

                // Position accidentals to the left, stagger if needed
                var xOffset = -1.5 * context.StaffSpace - accidentalColumnOffset;
                chordSymbol.AccidentalXOffsets.Add(xOffset);
                chordSymbol.AccidentalYPositions.Add(yPositions[i]);

                chordSymbol.AccidentalShifts.Add(true);
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
                if (measureAccidentals.ContainsKey(midiNote))
                {
                    measureAccidentals[midiNote] = accidental;
                }
            }
        }
    }

    private static Accidental GetAccidental(Pitch pitch)
    {
        // Extract accidental from pitch
        // For now, determine from MIDI note vs. natural notes
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

    private static bool NeedsAccidental(int midiNote, Accidental accidental, Dictionary<int, Accidental> measureAccidentals)
    {
        // For MVP: display accidentals for all non-natural notes
        // TODO: Consider key signature

        if (accidental != Accidental.Natural)
        {
            // Always show sharps/flats
            return true;
        }

        // Check if a previous accidental in the measure affects this note
        if (measureAccidentals.TryGetValue(midiNote, out var previousAccidental))
        {
            // If previous was an accidental and this is natural, show natural sign
            return previousAccidental != Accidental.Natural;
        }

        return false;
    }
}
