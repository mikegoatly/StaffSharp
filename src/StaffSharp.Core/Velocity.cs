namespace StaffSharp;

/// <summary>
/// Represents a musical velocity (loudness) with validation.
/// </summary>
public readonly record struct Velocity(float Value) : IComparable<Velocity>, IComparable
{
    /// <summary>
    /// Creates a velocity with validation.
    /// </summary>
    /// <param name="value">Velocity value (0.0-1.0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside 0.0-1.0 range.</exception>
    public static Velocity Create(float value)
    {
        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Velocity must be between 0.0 and 1.0.");
        }
        
        return new Velocity(value);
    }

    /// <summary>
    /// MIDI velocity value (0-127).
    /// </summary>
    public int MidiVelocity => (int)(Value * 127);

    /// <summary>
    /// Creates a velocity from MIDI velocity (0-127).
    /// </summary>
    public static Velocity FromMidi(int midiVelocity)
    {
        if (midiVelocity < 0 || midiVelocity > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(midiVelocity));
        }

        return Create(midiVelocity / 127f);
    }

    /// <summary>
    /// Predefined velocities for common dynamics.
    /// </summary>
    public static readonly Velocity Pianissimo = Create(0.2f);
    public static readonly Velocity Piano = Create(0.4f);
    public static readonly Velocity MezzoPiano = Create(0.5f);
    public static readonly Velocity MezzoForte = Create(0.6f);
    public static readonly Velocity Forte = Create(0.8f);
    public static readonly Velocity Fortissimo = Create(1.0f);

    public override string ToString() => $"{Value:P0}";

    // Operators
    public static bool operator >(Velocity a, Velocity b) => a.Value > b.Value;
    public static bool operator <(Velocity a, Velocity b) => a.Value < b.Value;
    public static bool operator >=(Velocity a, Velocity b) => a.Value >= b.Value;
    public static bool operator <=(Velocity a, Velocity b) => a.Value <= b.Value;

    public static Velocity operator +(Velocity a, Velocity b) => Add(a, b);

    public static Velocity operator -(Velocity a, Velocity b) => Subtract(a, b);

    public static Velocity Subtract(Velocity left, Velocity right)
    {
        return Create(MathF.Max(left.Value - right.Value, 0.0f));
    }

    public int CompareTo(Velocity other)
    {
        return Value.CompareTo(other.Value);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is Velocity other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(Velocity)}");
    }

    public static Velocity Add(Velocity left, Velocity right)
    {
        return Create(MathF.Min(left.Value + right.Value, 1.0f));
    }
}
