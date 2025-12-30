namespace StaffSharp.Notation;

using StaffSharp;

public static class RationalExtensions
{
    public static SymbolicDuration FromRational(this Rational duration)
    {
        // Handle some special cases that don't fit the general pattern
        if (duration == Rational.Create(1, 16))
        {
            return new SymbolicDuration(NoteDurationBase.ThirtySecond);
        }

        // 1. Try standard duration (no tuplet)
        if (TryGetStandardDuration(duration, out var baseVal, out var dots))
        {
            return new SymbolicDuration(baseVal, dots);
        }

        // 2. Try with tuplets
        // We check common tuplets by multiplying the duration by the inverse of the tuplet ratio.
        // If the result is a standard duration, then we found it.
        var tuplets = new[] { Tuplet.Triplet, Tuplet.Quintuplet, Tuplet.Sextuplet, Tuplet.Septuplet };
        
        foreach (var tuplet in tuplets)
        {
            // Calculate unscaled duration: duration * (Actual / Normal)
            // e.g. for Triplet (3 in 2), we multiply by 3/2 to get the "normal" duration that was compressed
            var unscaled = duration * Rational.Create(tuplet.ActualNotes, tuplet.NormalNotes);
            
            if (TryGetStandardDuration(unscaled, out baseVal, out dots))
            {
                return new SymbolicDuration(baseVal, dots, tuplet);
            }
        }

        // Fallback to quarter note if no exact match found
        return SymbolicDuration.Quarter;
    }

    private static bool TryGetStandardDuration(Rational duration, out NoteDurationBase baseVal, out int dots)
    {
        baseVal = NoteDurationBase.Unspecified;
        dots = 0;

        int numerator = duration.Numerator;
        int denominator = duration.Denominator;

        // Denominator must be a power of 2
        if (!IsPowerOfTwo(denominator))
        {
            return false;
        }

        // Calculate initial exponent from denominator: 1/Den = 2^(-log2(Den))
        int exponent = -Log2(denominator);

        // Remove factors of 2 from numerator
        while (numerator != 0 && (numerator & 1) == 0)
        {
            numerator >>= 1;
            exponent++;
        }

        // Check if M matches a dot pattern (2^(d+1) - 1)
        switch (numerator)
        {
            case 1: dots = 0; break;
            case 3: dots = 1; break;
            case 7: dots = 2; break;
            case 15: dots = 3; break;
            default: return false;
        }

        // k is the base-2 exponent for the note duration base value (Whole=1=2^0, Half=2=2^1, ..., ThirtySecond=32=2^5).
        // After we factor out all powers of 2 from the numerator into `exponent`, the remaining duration can be written
        // in the form: duration = (2^(dots + 1) - 1) / 2^(2 - k), from which solving for k yields k = 2 - dots - exponent.
        int k = 2 - dots - exponent;

        // Check if k corresponds to a valid NoteDurationBase (Whole=1 to ThirtySecond=32)
        // k=0 -> 1, k=5 -> 32
        if (k >= 0 && k <= 5)
        {
            baseVal = (NoteDurationBase)(1 << k);
            return true;
        }

        return false;
    }

    private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;

    private static int Log2(int n)
    {
        int log = 0;
        while ((n >>= 1) > 0) log++;
        return log;
    }
}