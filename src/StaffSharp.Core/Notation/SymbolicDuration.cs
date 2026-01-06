namespace StaffSharp.Notation;

/// <summary>
/// Represents a symbolic note duration (e.g., quarter note, dotted half, triplet eighth).
/// </summary>
public readonly record struct SymbolicDuration
{
    public SymbolicDuration(NoteDurationBase baseValue, int dots = 0, Tuplet? tuplet = null)
    {
        Base = baseValue;
        Dots = dots;
        Tuplet = tuplet;
    }

    public NoteDurationBase Base { get; }
    public int Dots { get; }
    public Tuplet? Tuplet { get; }

    /// <summary>
    /// Converts to beats (quarter note = 1 beat).
    /// </summary>
    public Rational ToBeats()
    {
        // Base duration in quarter notes
        var baseBeats = Rational.Create(4, (int)Base);

        // Apply dots (each dot adds half the previous value)
        if (Dots > 0)
        {
            var multiplier = Rational.Create(1, 1);
            var dotValue = Rational.Create(1, 2);

            for (int i = 0; i < Dots; i++)
            {
                multiplier += dotValue;
                dotValue *= Rational.Create(1, 2);
            }

            baseBeats *= multiplier;
        }

        // Apply tuplet (e.g., triplet = 2/3 of normal duration)
        if (Tuplet != null)
        {
            baseBeats *= Rational.Create(Tuplet.NormalNotes, Tuplet.ActualNotes);
        }

        return baseBeats;
    }

    public static readonly SymbolicDuration Quarter = new(NoteDurationBase.Quarter);
    public static readonly SymbolicDuration Half = new(NoteDurationBase.Half);
    public static readonly SymbolicDuration Whole = new(NoteDurationBase.Whole);
    public static readonly SymbolicDuration Eighth = new(NoteDurationBase.Eighth);
    public static readonly SymbolicDuration Sixteenth = new(NoteDurationBase.Sixteenth);
    public static readonly SymbolicDuration ThirtySecond = new(NoteDurationBase.ThirtySecond);

    // Common tuplets
    public static readonly SymbolicDuration TripletEighth = new(NoteDurationBase.Eighth, 0, Tuplet.Triplet);
    public static readonly SymbolicDuration TripletQuarter = new(NoteDurationBase.Quarter, 0, Tuplet.Triplet);
    public static readonly SymbolicDuration TripletSixteenth = new(NoteDurationBase.Sixteenth, 0, Tuplet.Triplet);

    public override string ToString()
    {
        return Tuplet != null
            ? Dots > 0
                ? $"{Base} {new string('.', Dots)} in {Tuplet}"
                : $"{Base} in {Tuplet}"
            : Dots > 0
                ? $"{Base} {new string('.', Dots)}"
                : Base.ToString();
    }
}
