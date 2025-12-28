using System.Globalization;

namespace StaffSharp.Core;

/// <summary>
/// Represents an exact fractional value for precise musical time calculations.
/// </summary>
public readonly record struct Rational : IComparable<Rational>, IComparable
{
    private Rational(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public int Numerator { get; }
    public int Denominator { get; }

    /// <summary>
    /// Creates a rational number with automatic simplification.
    /// </summary>
    public static Rational Create(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            throw new ArgumentException("Denominator cannot be zero.", nameof(denominator));
        }

        // Handle negative denominators
        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        // Simplify to keep denominators bounded
        var gcd = GreatestCommonDivisor(Math.Abs(numerator), Math.Abs(denominator));
        return new Rational(numerator / gcd, denominator / gcd);
    }

    public double ToDouble() => (double)Numerator / Denominator;

    public static readonly Rational Zero = new(0, 1);

    public override string ToString() => Denominator == 1 ? Numerator.ToString(CultureInfo.InvariantCulture) : $"{Numerator}/{Denominator}";

    // Basic arithmetic
    public static Rational operator +(Rational a, Rational b)
    {
        return Add(a, b);
    }

    public static Rational operator *(Rational a, Rational b)
    {
        return Multiply(a, b);
    }

    // Comparison
    public int CompareTo(Rational other)
    {
        var leftProduct = (long)Numerator * other.Denominator;
        var rightProduct = (long)other.Numerator * Denominator;
        return leftProduct.CompareTo(rightProduct);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    public static Rational Multiply(Rational left, Rational right)
    {
        return Create(left.Numerator * right.Numerator, left.Denominator * right.Denominator);
    }

    public static Rational Add(Rational left, Rational right)
    {
        var numerator = (left.Numerator * right.Denominator) + (right.Numerator * left.Denominator);
        var denominator = left.Denominator * right.Denominator;
        return Create(numerator, denominator);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is Rational other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException("Object is not a Rational.", nameof(obj));
    }

    public static bool operator <(Rational left, Rational right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(Rational left, Rational right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(Rational left, Rational right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(Rational left, Rational right)
    {
        return left.CompareTo(right) >= 0;
    }
}
