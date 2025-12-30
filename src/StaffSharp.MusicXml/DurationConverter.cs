namespace StaffSharp.MusicXml;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Converts MusicXML tick-based durations to SymbolicDuration.
/// </summary>
internal static class DurationConverter
{
    /// <summary>
    /// Converts a MusicXML duration (in divisions/ticks) to a SymbolicDuration.
    /// </summary>
    /// <param name="durationInDivisions">The duration value from MusicXML (in ticks).</param>
    /// <param name="divisions">The divisions value (ticks per quarter note).</param>
    /// <param name="tuplet">Optional tuplet to apply to the duration.</param>
    /// <returns>A SymbolicDuration representing the duration.</returns>
    /// <remarks>
    /// MusicXML uses a tick-based duration system where:
    /// - divisions = number of ticks per quarter note
    /// - duration = duration in ticks
    ///
    /// Example: divisions=4, duration=6
    /// - beats = 6/4 = 1.5 quarters = dotted quarter
    ///
    /// The conversion process:
    /// 1. Calculate duration as Rational: durationInDivisions / divisions
    /// 2. Use FromRational() to convert to SymbolicDuration
    /// 3. Apply tuplet if provided
    /// </remarks>
    public static SymbolicDuration Convert(int durationInDivisions, int divisions, Tuplet? tuplet = null)
    {
        if (divisions <= 0)
        {
            throw new ArgumentException("Divisions must be positive.", nameof(divisions));
        }

        if (durationInDivisions < 0)
        {
            throw new ArgumentException("Duration cannot be negative.", nameof(durationInDivisions));
        }

        // Convert to beats (quarter notes)
        var beats = Rational.Create(durationInDivisions, divisions);

        // Convert to symbolic duration
        var symbolicDuration = beats.FromRational();

        // Apply tuplet if provided
        // Note: If the MusicXML already encoded tuplet in the duration, we don't need this.
        // This is for cases where we need to apply tuplet separately.
        if (tuplet != null && symbolicDuration.Tuplet == null)
        {
            symbolicDuration = new SymbolicDuration(symbolicDuration.Base, symbolicDuration.Dots, tuplet);
        }

        return symbolicDuration;
    }
}
