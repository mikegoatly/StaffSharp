namespace StaffSharp.Core.Tests.Notation;

using StaffSharp.Core.Notation;

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
}
