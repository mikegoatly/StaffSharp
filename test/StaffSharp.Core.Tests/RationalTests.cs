namespace StaffSharp.Core.Tests;

public class RationalTests
{
    [Fact]
    public void Create_ValidFraction_CreatesInstance()
    {
        var r = Rational.Create(1, 2);
        Assert.Equal(1, r.Numerator);
        Assert.Equal(2, r.Denominator);
    }

    [Fact]
    public void Create_ZeroDenominator_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Rational.Create(1, 0));
    }

    [Fact]
    public void Create_AutomaticallySimplifies()
    {
        var r = Rational.Create(2, 4);
        Assert.Equal(1, r.Numerator);
        Assert.Equal(2, r.Denominator);
    }

    [Fact]
    public void Add_TwoFractions_ReturnsCorrectSum()
    {
        var a = Rational.Create(1, 2);
        var b = Rational.Create(1, 3);
        var result = a + b;

        Assert.Equal(Rational.Create(5, 6), result);
    }

    [Fact]
    public void Multiply_TwoFractions_ReturnsCorrectProduct()
    {
        var a = Rational.Create(2, 3);
        var b = Rational.Create(3, 4);
        var result = a * b;

        Assert.Equal(Rational.Create(1, 2), result);
    }

    [Fact]
    public void ToDouble_ConvertsCorrectly()
    {
        var r = Rational.Create(1, 4);
        Assert.Equal(0.25, r.ToDouble());
    }

    [Fact]
    public void CompareTo_WorksCorrectly()
    {
        var half = Rational.Create(1, 2);
        var quarter = Rational.Create(1, 4);

        Assert.True(half.CompareTo(quarter) > 0);
        Assert.True(quarter.CompareTo(half) < 0);
    }
}
