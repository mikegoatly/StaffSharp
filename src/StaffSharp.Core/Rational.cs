using System.Globalization;

namespace StaffSharp;

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

    public static Rational operator -(Rational a, Rational b)
    {
        return Subtract(a, b);
    }

    public static Rational operator /(Rational a, Rational b)
    {
        return Divide(a, b);
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

    public static Rational Subtract(Rational left, Rational right)
    {
        var numerator = (left.Numerator * right.Denominator) - (right.Numerator * left.Denominator);
        var denominator = left.Denominator * right.Denominator;
        return Create(numerator, denominator);
    }

    public static Rational Divide(Rational left, Rational right)
    {
        if (right.Numerator == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero rational.");
        }
        return Create(left.Numerator * right.Denominator, left.Denominator * right.Numerator);
    }

    /// <summary>
    /// Creates a rational approximation of a double value.
    /// Uses continued fractions with a maximum denominator to avoid overflow.
    /// </summary>
    public static Rational FromDouble(double value, int maxDenominator = 10000)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("Cannot convert NaN or Infinity to Rational.", nameof(value));
        }

        var sign = value >= 0 ? 1 : -1;
        value = Math.Abs(value);

        // Handle near-zero values
        if (value < 1e-10)
        {
            return Zero;
        }

        // Simple continued fractions implementation
        long p0 = 0, p1 = 1, q0 = 1, q1 = 0;
        double r = value;

        while (q1 <= maxDenominator)
        {
            long a = (long)Math.Floor(r);

            long p2 = a * p1 + p0;
            long q2 = a * q1 + q0;

            if (q2 > maxDenominator)
            {
                break;
            }

            // Check if we've found exact or close enough match
            if (Math.Abs((double)p2 / q2 - value) < 1e-10)
            {
                return Create(sign * (int)p2, (int)q2);
            }

            double nextR = r - a;
            if (Math.Abs(nextR) < 1e-10)
            {
                return Create(sign * (int)p2, (int)q2);
            }

            r = 1.0 / nextR;

            p0 = p1;
            p1 = p2;
            q0 = q1;
            q1 = q2;
        }

        // Return best approximation found
        return Create(sign * (int)p1, (int)q1);
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

    public static Rational Abs(Rational value)
    {
        return value >= Zero ? value : Zero - value;
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
