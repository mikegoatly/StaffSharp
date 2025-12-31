namespace StaffSharp;

/// <summary>
/// Represents a MIDI note number with validation.
/// Supports fractional values for microtonal music.
/// </summary>
public readonly record struct MidiNote(float Value) : IComparable<MidiNote>, IComparable
{
    /// <summary>
    /// Creates a MIDI note with validation.
    /// </summary>
    /// <param name="value">MIDI note number (0-127, fractional for microtones).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside valid range.</exception>
    public static MidiNote Create(float value)
    {
        if (value < 0 || value > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "MIDI note must be between 0 and 127.");
        }

        return new MidiNote(value);
    }

    /// <summary>
    /// Gets the integer MIDI note number (rounded).
    /// </summary>
    public int MidiNumber => (int)Math.Round(Value);

    /// <summary>
    /// Gets the pitch class (0-11, where 0=C, 1=C#, etc.).
    /// </summary>
    public PitchClass PitchClass => (PitchClass)(MidiNumber % 12);

    /// <summary>
    /// Gets the octave number (C4 = 60, so octave 4).
    /// </summary>
    public int Octave => (MidiNumber / 12) - 1;

    /// <summary>
    /// Converts to frequency in Hz using A4 = 440Hz.
    /// </summary>
    public Frequency ToFrequency() => Frequency.Create(MathF.Pow(2, (Value - 69) / 12) * 440);

    /// <summary>
    /// Creates a MIDI note from a frequency in Hz using A4 = 440Hz.
    /// Uses the formula: MIDI = 12 * log2(freq / 440) + 69
    /// </summary>
    /// <param name="frequencyHz">The frequency in Hz.</param>
    /// <returns>The nearest MIDI note.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when frequency results in invalid MIDI note.</exception>
    public static MidiNote FromFrequency(double frequencyHz)
    {
        if (frequencyHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frequencyHz), "Frequency must be positive.");
        }

        var midiNote = 12.0 * Math.Log2(frequencyHz / 440.0) + 69.0;
        return Create((float)Math.Round(midiNote));
    }

    /// <summary>
    /// Creates a MIDI note from a pitch class and octave.
    /// </summary>
    public static MidiNote FromPitchClass(PitchClass pitchClass, int octave)
    {
        if (pitchClass < PitchClass.C || pitchClass > PitchClass.B)
        {
            throw new ArgumentOutOfRangeException(nameof(pitchClass));
        }

        return Create((octave + 1) * 12 + (int)pitchClass);
    }

    public override string ToString() => $"MIDI {Value:F2}";

    // Operators
    public static MidiNote operator +(MidiNote left, MidiNote right) => Add(left, right);
    public static MidiNote operator -(MidiNote left, MidiNote right) => Subtract(left, right);
    public static MidiNote operator +(MidiNote note, float semitones) => Add(note, Create(semitones));
    public static MidiNote operator -(MidiNote note, float semitones) => Subtract(note, Create(semitones));

    public static bool operator >(MidiNote a, MidiNote b) => a.Value > b.Value;
    public static bool operator <(MidiNote a, MidiNote b) => a.Value < b.Value;
    public static bool operator >=(MidiNote a, MidiNote b) => a.Value >= b.Value;
    public static bool operator <=(MidiNote a, MidiNote b) => a.Value <= b.Value;

    public static MidiNote Subtract(MidiNote left, MidiNote right)
    {
        return Create(left.Value - right.Value);
    }

    public static MidiNote Add(MidiNote left, MidiNote right)
    {
        return Create(left.Value + right.Value);
    }

    // IComparable implementation
    public int CompareTo(MidiNote other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is MidiNote other)
        {
            return CompareTo(other);
        }
        
        throw new ArgumentException($"Object must be of type {nameof(MidiNote)}");
    }

    // Well-known MIDI notes (A2 to G6)

    // Octave 2
    public static MidiNote A2 => Create(45);
    public static MidiNote ASharp2 => Create(46);
    public static MidiNote BFlat2 => Create(46);
    public static MidiNote B2 => Create(47);

    // Octave 3
    public static MidiNote C3 => Create(48);
    public static MidiNote CSharp3 => Create(49);
    public static MidiNote DFlat3 => Create(49);
    public static MidiNote D3 => Create(50);
    public static MidiNote DSharp3 => Create(51);
    public static MidiNote EFlat3 => Create(51);
    public static MidiNote E3 => Create(52);
    public static MidiNote F3 => Create(53);
    public static MidiNote FSharp3 => Create(54);
    public static MidiNote GFlat3 => Create(54);
    public static MidiNote G3 => Create(55);
    public static MidiNote GSharp3 => Create(56);
    public static MidiNote AFlat3 => Create(56);
    public static MidiNote A3 => Create(57);
    public static MidiNote ASharp3 => Create(58);
    public static MidiNote BFlat3 => Create(58);
    public static MidiNote B3 => Create(59);

    // Octave 4 (Middle C octave)
    public static MidiNote C4 => Create(60);
    public static MidiNote CSharp4 => Create(61);
    public static MidiNote DFlat4 => Create(61);
    public static MidiNote D4 => Create(62);
    public static MidiNote DSharp4 => Create(63);
    public static MidiNote EFlat4 => Create(63);
    public static MidiNote E4 => Create(64);
    public static MidiNote F4 => Create(65);
    public static MidiNote FSharp4 => Create(66);
    public static MidiNote GFlat4 => Create(66);
    public static MidiNote G4 => Create(67);
    public static MidiNote GSharp4 => Create(68);
    public static MidiNote AFlat4 => Create(68);

    /// <summary>
    /// A4 (440 Hz) MIDI note. Concert pitch reference.
    /// </summary>
    public static MidiNote A4 => Create(69);
    public static MidiNote ASharp4 => Create(70);
    public static MidiNote BFlat4 => Create(70);
    public static MidiNote B4 => Create(71);

    // Octave 5
    public static MidiNote C5 => Create(72);
    public static MidiNote CSharp5 => Create(73);
    public static MidiNote DFlat5 => Create(73);
    public static MidiNote D5 => Create(74);
    public static MidiNote DSharp5 => Create(75);
    public static MidiNote EFlat5 => Create(75);
    public static MidiNote E5 => Create(76);
    public static MidiNote F5 => Create(77);
    public static MidiNote FSharp5 => Create(78);
    public static MidiNote GFlat5 => Create(78);
    public static MidiNote G5 => Create(79);
    public static MidiNote GSharp5 => Create(80);
    public static MidiNote AFlat5 => Create(80);
    public static MidiNote A5 => Create(81);
    public static MidiNote ASharp5 => Create(82);
    public static MidiNote BFlat5 => Create(82);
    public static MidiNote B5 => Create(83);

    // Octave 6
    public static MidiNote C6 => Create(84);
    public static MidiNote CSharp6 => Create(85);
    public static MidiNote DFlat6 => Create(85);
    public static MidiNote D6 => Create(86);
    public static MidiNote DSharp6 => Create(87);
    public static MidiNote EFlat6 => Create(87);
    public static MidiNote E6 => Create(88);
    public static MidiNote F6 => Create(89);
    public static MidiNote FSharp6 => Create(90);
    public static MidiNote GFlat6 => Create(90);
    public static MidiNote G6 => Create(91);
}

