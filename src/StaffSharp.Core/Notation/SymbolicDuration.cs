namespace StaffSharp.Core.Notation;

/// <summary>
/// Represents a symbolic note duration (e.g., quarter note, dotted half).
/// </summary>
public readonly record struct SymbolicDuration
{
    public SymbolicDuration(NoteDurationBase baseValue, int dots = 0)
    {
        Base = baseValue;
        Dots = dots;
    }

    public NoteDurationBase Base { get; }
    public int Dots { get; }

    /// <summary>
    /// Converts to beats (quarter note = 1 beat).
    /// </summary>
    public Rational ToBeats()
    {
        // Base duration in quarter notes
        var baseBeats = Rational.Create(4, (int)Base);

        // Apply dots (each dot adds half the previous value)
        if (Dots == 0)
        {
            return baseBeats;
        }

        var multiplier = Rational.Create(1, 1);
        var dotValue = Rational.Create(1, 2);

        for (int i = 0; i < Dots; i++)
        {
            multiplier = multiplier + dotValue;
            dotValue = dotValue * Rational.Create(1, 2);
        }

        return baseBeats * multiplier;
    }

    public static readonly SymbolicDuration Quarter = new(NoteDurationBase.Quarter);
    public static readonly SymbolicDuration Half = new(NoteDurationBase.Half);
    public static readonly SymbolicDuration Whole = new(NoteDurationBase.Whole);
    public static readonly SymbolicDuration Eighth = new(NoteDurationBase.Eighth);
}
