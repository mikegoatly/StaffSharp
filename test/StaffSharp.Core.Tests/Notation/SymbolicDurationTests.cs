namespace StaffSharp.Core.Tests.Notation;

using StaffSharp;
using StaffSharp.Notation;

public class SymbolicDurationTests
{
    [Fact]
    public void ToBeats_Quarter_ReturnsOne()
    {
        var quarter = SymbolicDuration.Quarter;
        Assert.Equal(Rational.Create(1, 1), quarter.ToBeats());
    }

    [Fact]
    public void ToBeats_Half_ReturnsTwo()
    {
        var half = SymbolicDuration.Half;
        Assert.Equal(Rational.Create(2, 1), half.ToBeats());
    }

    [Fact]
    public void ToBeats_Whole_ReturnsFour()
    {
        var whole = SymbolicDuration.Whole;
        Assert.Equal(Rational.Create(4, 1), whole.ToBeats());
    }

    [Fact]
    public void ToBeats_Eighth_ReturnsHalf()
    {
        var eighth = SymbolicDuration.Eighth;
        Assert.Equal(Rational.Create(1, 2), eighth.ToBeats());
    }

    [Fact]
    public void ToBeats_DottedQuarter_ReturnsThreeHalves()
    {
        var dottedQuarter = new SymbolicDuration(NoteDurationBase.Quarter, dots: 1);
        Assert.Equal(Rational.Create(3, 2), dottedQuarter.ToBeats());
    }

    [Fact]
    public void ToBeats_DottedHalf_ReturnsThree()
    {
        var dottedHalf = new SymbolicDuration(NoteDurationBase.Half, dots: 1);
        Assert.Equal(Rational.Create(3, 1), dottedHalf.ToBeats());
    }

    [Fact]
    public void ToBeats_DoubleDottedQuarter_ReturnsCorrectValue()
    {
        // Double dotted quarter = 1 + 1/2 + 1/4 = 7/4
        var doubleDottedQuarter = new SymbolicDuration(NoteDurationBase.Quarter, dots: 2);
        Assert.Equal(Rational.Create(7, 4), doubleDottedQuarter.ToBeats());
    }

    [Fact]
    public void ToBeats_TripletEighth_ReturnsOneThird()
    {
        // Triplet eighth = eighth note * (2/3) = 1/2 * 2/3 = 1/3 beat
        var tripletEighth = SymbolicDuration.TripletEighth;
        Assert.Equal(Rational.Create(1, 3), tripletEighth.ToBeats());
    }

    [Fact]
    public void ToBeats_TripletQuarter_ReturnsTwoThirds()
    {
        // Triplet quarter = quarter note * (2/3) = 1 * 2/3 = 2/3 beat
        var tripletQuarter = SymbolicDuration.TripletQuarter;
        Assert.Equal(Rational.Create(2, 3), tripletQuarter.ToBeats());
    }

    [Fact]
    public void ToBeats_TripletSixteenth_ReturnsSixth()
    {
        // Triplet sixteenth = sixteenth note * (2/3) = 1/4 * 2/3 = 1/6 beat
        var tripletSixteenth = SymbolicDuration.TripletSixteenth;
        Assert.Equal(Rational.Create(1, 6), tripletSixteenth.ToBeats());
    }

    [Fact]
    public void ToBeats_CustomTuplet_CalculatesCorrectly()
    {
        // Quintuplet eighth = eighth note * (4/5) = 1/2 * 4/5 = 2/5 beat
        var quintupletEighth = new SymbolicDuration(NoteDurationBase.Eighth, 0, Tuplet.Quintuplet);
        Assert.Equal(Rational.Create(2, 5), quintupletEighth.ToBeats());
    }

    [Fact]
    public void ToBeats_DottedTriplet_CombinesDotsAndTuplet()
    {
        // Dotted triplet eighth = (1/2 * 1.5) * (2/3) = (3/4) * (2/3) = 1/2 beat
        var dottedTripletEighth = new SymbolicDuration(NoteDurationBase.Eighth, dots: 1, tuplet: Tuplet.Triplet);
        Assert.Equal(Rational.Create(1, 2), dottedTripletEighth.ToBeats());
    }
}
