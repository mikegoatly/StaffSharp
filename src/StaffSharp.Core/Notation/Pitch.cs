namespace StaffSharp.Core.Notation;

/// <summary>
/// Represents a musical pitch with pitch class, octave, and optional accidental.
/// </summary>
public readonly record struct Pitch
{
    public Pitch(PitchClass pitchClass, int octave, Accidental? accidental = null)
    {
        PitchClass = pitchClass;
        Octave = octave;
        Accidental = accidental;
    }

    public PitchClass PitchClass { get; }
    public int Octave { get; }
    public Accidental? Accidental { get; }

    /// <summary>
    /// Converts to MIDI note number.
    /// </summary>
    public MidiNote ToMidiNote()
    {
        var midiNumber = (Octave + 1) * 12 + (int)PitchClass;

        // Apply accidental
        if (Accidental == Notation.Accidental.Sharp)
        {
            midiNumber++;
        }
        else if (Accidental == Notation.Accidental.Flat)
        {
            midiNumber--;
        }

        return MidiNote.Create(midiNumber);
    }
}
