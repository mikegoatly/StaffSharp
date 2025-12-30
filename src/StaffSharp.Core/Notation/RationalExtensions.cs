namespace StaffSharp.Notation;

using StaffSharp;

public static class RationalExtensions
{
    public static SymbolicDuration FromRational(this Rational duration)
    {
        // Try to match common durations
        // Quarter note = 1/1, half = 2/1, whole = 4/1, eighth = 1/2, etc.

        switch (duration.Denominator)
        {
            case 1:
                return duration.Numerator switch
                {
                    4 => SymbolicDuration.Whole,
                    2 => SymbolicDuration.Half,
                    3 => new SymbolicDuration(NoteDurationBase.Half, dots: 1),
                    // Default if 1 or can't match
                    _ => SymbolicDuration.Quarter,
                };
            case 2:
                return duration.Numerator switch
                {
                    1 => SymbolicDuration.Eighth,
                    3 => new SymbolicDuration(NoteDurationBase.Quarter, dots: 1),
                    // Default if we can't match
                    _ => SymbolicDuration.Quarter
                };
            case 4:
                return duration.Numerator switch
                {
                    1 => SymbolicDuration.Sixteenth,
                    3 => new SymbolicDuration(NoteDurationBase.Eighth, dots: 1),
                    7 => new SymbolicDuration(NoteDurationBase.Quarter, dots: 2), // Double dotted quarter
                    // Default if we can't match
                    _ => SymbolicDuration.Quarter
                };
            case 8:
                return duration.Numerator switch
                {
                    1 => SymbolicDuration.Eighth,
                    2 => SymbolicDuration.Quarter,
                    3 => new SymbolicDuration(NoteDurationBase.Eighth, dots: 1),
                    4 => SymbolicDuration.Half,
                    6 => new SymbolicDuration(NoteDurationBase.Half, dots: 1),
                    7 => new SymbolicDuration(NoteDurationBase.Eighth, dots: 2), // Double dotted eighth
                    14 => new SymbolicDuration(NoteDurationBase.Half, dots: 2), // Double dotted half
                    15 => new SymbolicDuration(NoteDurationBase.Eighth, dots: 3), // Triple dotted eighth
                    // Default if we can't match
                    _ => SymbolicDuration.Quarter
                };
            case 3:
                // Triplets: 1/3 beat = triplet eighth, 2/3 = triplet quarter
                return duration.Numerator switch
                {
                    1 => SymbolicDuration.TripletEighth,  // 1/3 beat
                    2 => SymbolicDuration.TripletQuarter, // 2/3 beat
                    4 => new SymbolicDuration(NoteDurationBase.Half, 0, Tuplet.Triplet), // 4/3 beat = triplet half
                    // Default
                    _ => SymbolicDuration.Quarter
                };
            case 5:
                // Quintuplets
                return duration.Numerator switch
                {
                    2 => new SymbolicDuration(NoteDurationBase.Eighth, 0, Tuplet.Quintuplet), // 2/5 beat
                    4 => new SymbolicDuration(NoteDurationBase.Quarter, 0, Tuplet.Quintuplet), // 4/5 beat
                    // Default
                    _ => SymbolicDuration.Quarter
                };
            case 6:
                // Could come from triplet sixteenths or dotted triplets
                return duration.Numerator switch
                {
                    1 => SymbolicDuration.TripletSixteenth, // 1/6 beat
                    // Default
                    _ => SymbolicDuration.Quarter
                };
            case 16:
                return duration.Numerator switch
                {
                    1 => new SymbolicDuration(NoteDurationBase.ThirtySecond), // 1/16 beat = 32nd note
                    2 => SymbolicDuration.Sixteenth, // 2/16 = 1/8 beat
                    3 => new SymbolicDuration(NoteDurationBase.Sixteenth, dots: 1), // 3/16 beat = dotted 16th
                    // Default
                    _ => SymbolicDuration.Quarter
                };
            default:
                // Default if we can't match
                return SymbolicDuration.Quarter;
        }
    }
}