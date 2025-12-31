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
    public void Subtract_TwoFractions_ReturnsCorrectDifference()
    {
        var a = Rational.Create(5, 6);
        var b = Rational.Create(1, 3);
        var result = a - b;

        // 5/6 - 1/3 = 5/6 - 2/6 = 3/6 = 1/2
        Assert.Equal(Rational.Create(1, 2), result);
    }

    [Fact]
    public void Subtract_ResultIsNegative_HandlesCorrectly()
    {
        var a = Rational.Create(1, 4);
        var b = Rational.Create(1, 2);
        var result = a - b;

        // 1/4 - 1/2 = 1/4 - 2/4 = -1/4
        Assert.Equal(Rational.Create(-1, 4), result);
    }

    [Fact]
    public void Divide_TwoFractions_ReturnsCorrectQuotient()
    {
        var a = Rational.Create(1, 2);
        var b = Rational.Create(1, 4);
        var result = a / b;

        // (1/2) / (1/4) = (1/2) * (4/1) = 4/2 = 2
        Assert.Equal(Rational.Create(2, 1), result);
    }

    [Fact]
    public void Divide_ByZero_ThrowsException()
    {
        var a = Rational.Create(1, 2);
        var b = Rational.Create(0, 1);

        Assert.Throws<DivideByZeroException>(() => a / b);
    }

    [Fact]
    public void Divide_ComplexFractions_SimplifiesResult()
    {
        var a = Rational.Create(3, 4);
        var b = Rational.Create(2, 3);
        var result = a / b;

        // (3/4) / (2/3) = (3/4) * (3/2) = 9/8
        Assert.Equal(Rational.Create(9, 8), result);
    }

    [Fact]
    public void FromDouble_WholeNumber_ConvertsExactly()
    {
        var result = Rational.FromDouble(5.0);

        Assert.Equal(5, result.Numerator);
        Assert.Equal(1, result.Denominator);
    }

    [Fact]
    public void FromDouble_SimpleDecimal_ConvertsCorrectly()
    {
        var result = Rational.FromDouble(0.5);

        Assert.Equal(Rational.Create(1, 2), result);
    }

    [Fact]
    public void FromDouble_OneThird_ConvertsToApproximation()
    {
        var result = Rational.FromDouble(1.0 / 3.0);

        // Should approximate 1/3 closely
        Assert.Equal(1, result.Numerator);
        Assert.Equal(3, result.Denominator);
    }

    [Fact]
    public void FromDouble_DottedQuarter_ConvertsCorrectly()
    {
        // Dotted quarter = 1.5 beats = 3/2
        var result = Rational.FromDouble(1.5);

        Assert.Equal(3, result.Numerator);
        Assert.Equal(2, result.Denominator);
    }

    [Fact]
    public void FromDouble_TripletEighth_ConvertsCorrectly()
    {
        // Triplet eighth = 1/3 beat
        var result = Rational.FromDouble(1.0 / 3.0);

        Assert.Equal(1, result.Numerator);
        Assert.Equal(3, result.Denominator);
    }

    [Fact]
    public void FromDouble_NegativeValue_PreservesSign()
    {
        var result = Rational.FromDouble(-2.5);

        Assert.Equal(-5, result.Numerator);
        Assert.Equal(2, result.Denominator);
    }

    [Fact]
    public void FromDouble_NaN_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Rational.FromDouble(double.NaN));
    }

    [Fact]
    public void FromDouble_Infinity_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Rational.FromDouble(double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => Rational.FromDouble(double.NegativeInfinity));
    }

    [Fact]
    public void FromDouble_VerySmallValue_RoundsToZero()
    {
        var result = Rational.FromDouble(1e-12);

        Assert.Equal(Rational.Zero, result);
    }

    [Fact]
    public void FromDouble_RespectMaxDenominator()
    {
        // With small max denominator, should use simpler approximation
        var result = Rational.FromDouble(Math.PI, maxDenominator: 10);

        // Should get a simple approximation like 22/7 or similar
        Assert.True(result.Denominator <= 10);
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
