namespace StaffSharp.Abc.Tests.Exporting;

using StaffSharp.Abc.Exporting;
using StaffSharp.Notation;

public class AbcBarlineFormatterTests
{
    [Fact]
    public void Format_NormalBarline_ReturnsPipe()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.Normal, null);
        Assert.Equal("|", result);
    }

    [Fact]
    public void Format_DoubleBar_ReturnsDoublePipe()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.DoubleBar, null);
        Assert.Equal("||", result);
    }

    [Fact]
    public void Format_FinalBarline_ReturnsPipeBracket()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.Final, null);
        Assert.Equal("|]", result);
    }

    [Fact]
    public void Format_RepeatEnd_ReturnsColonPipe()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.RepeatEnd, null);
        Assert.Equal(":|", result);
    }

    [Fact]
    public void Format_RepeatStart_ReturnsPipeColon()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.Normal, BarlineType.RepeatStart);
        Assert.Equal("|:", result);
    }

    [Fact]
    public void Format_RepeatBoth_ReturnsDoubleColon()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.RepeatEnd, BarlineType.RepeatStart);
        Assert.Equal("::", result);
    }

    [Fact]
    public void Format_NullBarline_ReturnsPipe()
    {
        var result = AbcBarlineFormatter.Format(null, null);
        Assert.Equal("|", result);
    }

    [Fact]
    public void Format_WithSingleVariant_ReturnsBracketNumber()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.Normal, null, [1]);
        Assert.Equal("[1|", result);
    }

    [Fact]
    public void Format_WithMultipleVariants_ReturnsBracketNumbers()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.Normal, null, [1, 3]);
        Assert.Equal("[1,3|", result);
    }

    [Fact]
    public void Format_VariantWithRepeatEnd_CombinesBoth()
    {
        var result = AbcBarlineFormatter.Format(BarlineType.RepeatEnd, null, [2]);
        Assert.Equal("[2:|", result);
    }
}
