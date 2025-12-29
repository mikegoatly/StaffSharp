namespace StaffSharp;

/// <summary>
/// Represents a frequency in Hz with validation.
/// </summary>
public readonly record struct Frequency : IComparable<Frequency>, IComparable
{
    private Frequency(float value)
    {
        Value = value;
    }

    public readonly float Value { get; }

    /// <summary>
    /// Creates a frequency with validation.
    /// </summary>
    /// <param name="value">Frequency in Hz (must be positive).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not positive.</exception>
    public static Frequency Create(float value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Frequency must be positive.");
        }
        
        return new Frequency(value);
    }

    /// <summary>
    /// Converts to MIDI note number using A4 = 440Hz.
    /// </summary>
    public MidiNote ToMidiNote() => MidiNote.Create(69 + 12 * MathF.Log2(Value / 440));

    /// <summary>
    /// Creates a frequency from a MIDI note.
    /// </summary>
    public static Frequency FromMidiNote(MidiNote midiNote) => midiNote.ToFrequency();

    /// <summary>
    /// Common frequencies for reference.
    /// </summary>
    public static readonly Frequency A4 = Create(440);

    /// <summary>
    /// Middle C (C4) frequency.
    /// </summary>
    public static readonly Frequency C4 = Create(261.63f);

    public override string ToString() => $"{Value:F1} Hz";

    // Operators for convenient math
    public static Frequency operator +(Frequency a, Frequency b) => Add(a, b);
    public static Frequency operator -(Frequency a, Frequency b) => Subtract(a, b);
    public static Frequency operator *(Frequency a, Frequency b) => Multiply(a, b);
    public static Frequency operator /(Frequency a, Frequency b) => Divide(a, b);
    public static Frequency operator +(Frequency a, float add) => Add(a, Create(add));
    public static Frequency operator -(Frequency a, float subtract) => Subtract(a, Create(subtract));
    public static Frequency operator *(Frequency a, float factor) => Multiply(a, Create(factor));
    public static Frequency operator /(Frequency a, float divisor) => Divide(a, Create(divisor));
    public static bool operator >(Frequency a, Frequency b) => a.Value > b.Value;
    public static bool operator <(Frequency a, Frequency b) => a.Value < b.Value;
    public static bool operator >=(Frequency a, Frequency b) => a.Value >= b.Value;
    public static bool operator <=(Frequency a, Frequency b) => a.Value <= b.Value;

    public static Frequency Add(Frequency left, Frequency right)
    {
        return Create(left.Value + right.Value);
    }

    public static Frequency Subtract(Frequency left, Frequency right)
    {
        return Create(left.Value - right.Value);
    }

    public static Frequency Multiply(Frequency left, Frequency right)
    {
        return Create(left.Value * right.Value);
    }

    public static Frequency Divide(Frequency left, Frequency right)
    {
        // No need to worry about divide by zero; we guard against Frequencies with <= zero in Create
        return Create(left.Value / right.Value);
    }

    public int CompareTo(Frequency other)
    {
        return Value.CompareTo(other.Value);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is Frequency other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(Frequency)}");
    }
}