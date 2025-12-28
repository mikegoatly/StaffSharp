namespace StaffSharp.Core.Tests;

public class PitchClassTests
{
    [Fact]
    public void PitchClass_HasCorrectValues()
    {
        Assert.Equal(0, (int)PitchClass.C);
        Assert.Equal(1, (int)PitchClass.CSharp);
        Assert.Equal(2, (int)PitchClass.D);
        Assert.Equal(3, (int)PitchClass.DSharp);
        Assert.Equal(4, (int)PitchClass.E);
        Assert.Equal(5, (int)PitchClass.F);
        Assert.Equal(6, (int)PitchClass.FSharp);
        Assert.Equal(7, (int)PitchClass.G);
        Assert.Equal(8, (int)PitchClass.GSharp);
        Assert.Equal(9, (int)PitchClass.A);
        Assert.Equal(10, (int)PitchClass.ASharp);
        Assert.Equal(11, (int)PitchClass.B);
    }

    [Fact]
    public void GetName_ReturnsCorrectNames()
    {
        Assert.Equal("C", PitchClass.C.GetName());
        Assert.Equal("C#", PitchClass.CSharp.GetName());
        Assert.Equal("D", PitchClass.D.GetName());
        Assert.Equal("Eb", PitchClass.DSharp.GetName());
        Assert.Equal("E", PitchClass.E.GetName());
        Assert.Equal("F", PitchClass.F.GetName());
        Assert.Equal("F#", PitchClass.FSharp.GetName());
        Assert.Equal("G", PitchClass.G.GetName());
        Assert.Equal("Ab", PitchClass.GSharp.GetName());
        Assert.Equal("A", PitchClass.A.GetName());
        Assert.Equal("Bb", PitchClass.ASharp.GetName());
        Assert.Equal("B", PitchClass.B.GetName());
    }

    [Fact]
    public void GetName_InvalidPitchClass_ReturnsQuestionMark()
    {
        var invalidPitchClass = (PitchClass)99;
        Assert.Equal("?", invalidPitchClass.GetName());
    }
}
