namespace StaffSharp.Notation;

/// <summary>
/// Represents a time signature (e.g., 4/4, 3/4).
/// </summary>
public record TimeSignature(int Numerator, int Denominator)
{
    /// <summary>
    /// Gets the number of beats per measure (in quarter notes).
    /// </summary>
    public Rational BeatsPerMeasure => Rational.Create(Numerator * 4, Denominator);

    public static readonly TimeSignature CommonTime = new(4, 4);
}
