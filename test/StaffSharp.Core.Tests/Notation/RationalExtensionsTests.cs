namespace StaffSharp.Core.Tests.Notation;

using StaffSharp;
using StaffSharp.Notation;

public class RationalExtensionsTests
{
    [Fact]
    public void FromRational_Whole_ReturnsWholeDuration()
    {
        var duration = Rational.Create(4, 1);
        Assert.Equal(SymbolicDuration.Whole, duration.FromRational());
    }

    [Fact]
    public void FromRational_Half_ReturnsHalfDuration()
    {
        var duration = Rational.Create(2, 1);
        Assert.Equal(SymbolicDuration.Half, duration.FromRational());
    }

    [Fact]
    public void FromRational_Quarter_ReturnsQuarterDuration()
    {
        var duration = Rational.Create(1, 1);
        Assert.Equal(SymbolicDuration.Quarter, duration.FromRational());
    }

    [Fact]
    public void FromRational_Eighth_ReturnsEighthDuration()
    {
        var duration = Rational.Create(1, 2);
        Assert.Equal(SymbolicDuration.Eighth, duration.FromRational());
    }

    [Fact]
    public void FromRational_Sixteenth_ReturnsSixteenthDuration()
    {
        var duration = Rational.Create(1, 4);
        Assert.Equal(SymbolicDuration.Sixteenth, duration.FromRational());
    }

    [Fact]
    public void FromRational_DottedHalf_ReturnsDottedHalfDuration()
    {
        var duration = Rational.Create(3, 1);
        var expected = new SymbolicDuration(NoteDurationBase.Half, dots: 1);
        Assert.Equal(expected, duration.FromRational());
    }

    [Fact]
    public void FromRational_DottedQuarter_ReturnsDottedQuarterDuration()
    {
        var duration = Rational.Create(3, 2);
        var expected = new SymbolicDuration(NoteDurationBase.Quarter, dots: 1);
        Assert.Equal(expected, duration.FromRational());
    }

    [Fact]
    public void FromRational_DottedEighth_ReturnsDottedEighthDuration()
    {
        var duration = Rational.Create(3, 4);
        var expected = new SymbolicDuration(NoteDurationBase.Eighth, dots: 1);
        Assert.Equal(expected, duration.FromRational());
    }

    [Fact]
    public void FromRational_UnmatchedDuration_ReturnsClosestApproximation()
    {
        // Test that unmatched durations return the closest standard duration
        // 5/7 ≈ 0.714 beats, closest match is dotted eighth (0.75) or eighth (0.5)
        // Since 0.75 - 0.714 = 0.036 and 0.714 - 0.5 = 0.214, dotted eighth is closer
        var duration = Rational.Create(5, 7);
        var result = duration.FromRational();

        // The improved algorithm finds the closest match (dotted eighth: 3/4 = 0.75)
        var expected = new SymbolicDuration(NoteDurationBase.Eighth, dots: 1);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromRational_ComplexDuration_FindsMatch()
    {
        // 5 beats = Triple dotted whole note (7.5 beats) in a triplet (2/3 duration)
        // 7.5 * 2/3 = 5
        var duration = Rational.Create(5, 1);
        var result = duration.FromRational();
        
        Assert.Equal(NoteDurationBase.Whole, result.Base);
        Assert.Equal(3, result.Dots);
        Assert.Equal(Tuplet.Triplet, result.Tuplet);
    }

    [Fact]
    public void FromRational_SimplifiedRationals_ConvertsCorrectly()
    {
        // Note: Rationals auto-simplify, so we test the simplified forms
        // These test conversions that might result from ABC parsing operations

        // 2/8 simplifies to 1/4 beat = sixteenth note
        Assert.Equal(SymbolicDuration.Sixteenth, Rational.Create(2, 8).FromRational());

        // 4/8 simplifies to 1/2 beat = eighth note
        Assert.Equal(SymbolicDuration.Eighth, Rational.Create(4, 8).FromRational());

        // 6/8 simplifies to 3/4 beat = dotted eighth
        Assert.Equal(new SymbolicDuration(NoteDurationBase.Eighth, dots: 1), Rational.Create(6, 8).FromRational());

        // 8/8 simplifies to 1 beat = quarter note
        Assert.Equal(SymbolicDuration.Quarter, Rational.Create(8, 8).FromRational());
    }

    [Fact]
    public void FromRational_Triplets_CreatesTuplets()
    {
        // 1/3 beat = triplet eighth
        Assert.Equal(SymbolicDuration.TripletEighth, Rational.Create(1, 3).FromRational());

        // 2/3 beat = triplet quarter
        Assert.Equal(SymbolicDuration.TripletQuarter, Rational.Create(2, 3).FromRational());

        // 1/6 beat = triplet sixteenth
        Assert.Equal(SymbolicDuration.TripletSixteenth, Rational.Create(1, 6).FromRational());
    }

    [Fact]
    public void FromRational_Quintuplets_CreatesTuplets()
    {
        // 2/5 beat = quintuplet eighth
        var quintupletEighth = Rational.Create(2, 5).FromRational();
        Assert.Equal(NoteDurationBase.Eighth, quintupletEighth.Base);
        Assert.Equal(Tuplet.Quintuplet, quintupletEighth.Tuplet);
    }

    [Fact]
    public void FromRational_ThirtySecondNotes_ConvertsCorrectly()
    {
        // 1/16 beat = 32nd note
        var thirtySecond = Rational.Create(1, 16).FromRational();
        Assert.Equal(NoteDurationBase.ThirtySecond, thirtySecond.Base);
        Assert.Equal(0, thirtySecond.Dots);
    }

    [Fact]
    public void FromRational_RoundTrip_PreservesStandardDurations()
    {
        // Test that converting SymbolicDuration -> Rational -> SymbolicDuration preserves the value
        var durations = new[]
        {
            SymbolicDuration.Whole,
            SymbolicDuration.Half,
            SymbolicDuration.Quarter,
            SymbolicDuration.Eighth,
            SymbolicDuration.Sixteenth
        };

        foreach (var duration in durations)
        {
            var rational = duration.ToBeats();
            var roundTrip = rational.FromRational();
            Assert.Equal(duration, roundTrip);
        }
    }

    [Fact]
    public void FromRational_RoundTrip_PreservesTriplets()
    {
        // Test triplet round-trip
        var triplets = new[]
        {
            SymbolicDuration.TripletEighth,
            SymbolicDuration.TripletQuarter,
            SymbolicDuration.TripletSixteenth
        };

        foreach (var duration in triplets)
        {
            var rational = duration.ToBeats();
            var roundTrip = rational.FromRational();
            Assert.Equal(duration, roundTrip);
        }
    }
}
