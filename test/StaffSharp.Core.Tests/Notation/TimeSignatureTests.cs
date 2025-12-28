namespace StaffSharp.Core.Tests.Notation;

using StaffSharp.Core.Notation;

public class TimeSignatureTests
{
    [Fact]
    public void BeatsPerMeasure_CommonTime_ReturnsFour()
    {
        var commonTime = TimeSignature.CommonTime;
        Assert.Equal(Rational.Create(4, 1), commonTime.BeatsPerMeasure);
    }

    [Fact]
    public void BeatsPerMeasure_ThreeFour_ReturnsThree()
    {
        var threeFour = new TimeSignature(3, 4);
        Assert.Equal(Rational.Create(3, 1), threeFour.BeatsPerMeasure);
    }

    [Fact]
    public void BeatsPerMeasure_SixEight_ReturnsThree()
    {
        // 6/8 = 6 eighth notes = 3 quarter notes
        var sixEight = new TimeSignature(6, 8);
        Assert.Equal(Rational.Create(3, 1), sixEight.BeatsPerMeasure);
    }

    [Fact]
    public void BeatsPerMeasure_TwoTwo_ReturnsTwo()
    {
        var twoTwo = new TimeSignature(2, 2);
        Assert.Equal(Rational.Create(4, 1), twoTwo.BeatsPerMeasure); // 2 half notes = 4 quarter notes
    }
}
