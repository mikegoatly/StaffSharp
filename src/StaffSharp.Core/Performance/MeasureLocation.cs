namespace StaffSharp.Performance;

/// <summary>
/// Represents a location within a musical score: which measure and beat within that measure.
/// </summary>
/// <param name="MeasureNumber">The measure number (1-indexed, first measure is 1).</param>
/// <param name="BeatInMeasure">The beat position within the measure (0-indexed from measure start).</param>
public readonly record struct MeasureLocation(
    int MeasureNumber,
    Rational BeatInMeasure);
