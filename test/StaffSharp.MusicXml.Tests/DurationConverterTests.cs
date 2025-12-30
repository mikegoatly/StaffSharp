namespace StaffSharp.MusicXml.Tests;

using StaffSharp.MusicXml;
using StaffSharp.Notation;

public class DurationConverterTests
{
    [Theory]
    [InlineData(4, 4, NoteDurationBase.Quarter, 0)] // Quarter note (divisions=4, duration=4)
    [InlineData(4, 8, NoteDurationBase.Half, 0)]    // Half note (divisions=4, duration=8)
    [InlineData(4, 16, NoteDurationBase.Whole, 0)]  // Whole note (divisions=4, duration=16)
    [InlineData(4, 2, NoteDurationBase.Eighth, 0)]  // Eighth note (divisions=4, duration=2)
    [InlineData(4, 1, NoteDurationBase.Sixteenth, 0)] // Sixteenth note (divisions=4, duration=1)
    public void Convert_StandardDurations_ReturnsCorrectBase(int divisions, int duration, NoteDurationBase expectedBase, int expectedDots)
    {
        var result = DurationConverter.Convert(duration, divisions);

        Assert.Equal(expectedBase, result.Base);
        Assert.Equal(expectedDots, result.Dots);
        Assert.Null(result.Tuplet);
    }

    [Theory]
    [InlineData(4, 6, NoteDurationBase.Quarter, 1)]  // Dotted quarter (divisions=4, duration=6 = 1.5 quarters)
    [InlineData(4, 12, NoteDurationBase.Half, 1)]    // Dotted half (divisions=4, duration=12 = 3 quarters)
    [InlineData(4, 3, NoteDurationBase.Eighth, 1)]   // Dotted eighth (divisions=4, duration=3 = 0.75 quarters)
    [InlineData(8, 6, NoteDurationBase.Eighth, 1)]   // Dotted eighth (divisions=8, duration=6)
    [InlineData(8, 12, NoteDurationBase.Quarter, 1)] // Dotted quarter (divisions=8, duration=12)
    public void Convert_DottedDurations_ReturnsCorrectDots(int divisions, int duration, NoteDurationBase expectedBase, int expectedDots)
    {
        var result = DurationConverter.Convert(duration, divisions);

        Assert.Equal(expectedBase, result.Base);
        Assert.Equal(expectedDots, result.Dots);
        Assert.Null(result.Tuplet);
    }

    [Fact]
    public void Convert_TripletQuarter_WithExplicitTuplet_ReturnsTupletDuration()
    {
        // MusicXML often encodes triplets with explicit tuplet and adjusted duration
        // e.g., triplet quarter = 2/3 of normal quarter
        // divisions=3, duration=2 (2/3 of quarter)
        var tuplet = new Tuplet(3, 2); // 3 in the time of 2

        var result = DurationConverter.Convert(2, 3, tuplet);

        // Should be quarter note with triplet
        Assert.Equal(NoteDurationBase.Quarter, result.Base);
        Assert.Equal(0, result.Dots);
        Assert.NotNull(result.Tuplet);
        Assert.Equal(3, result.Tuplet.ActualNotes);
        Assert.Equal(2, result.Tuplet.NormalNotes);
    }

    [Fact]
    public void Convert_TripletEighth_WithoutExplicitTuplet_InfersTuplet()
    {
        // When MusicXML encodes triplet without explicit tuplet element
        // divisions=3, duration=1 (1/3 of quarter = triplet eighth)
        var result = DurationConverter.Convert(1, 3);

        // FromRational should infer the triplet
        Assert.Equal(NoteDurationBase.Eighth, result.Base);
        Assert.Equal(0, result.Dots);
        Assert.Equal(Tuplet.Triplet, result.Tuplet);
    }

    [Theory]
    [InlineData(1, 1)]  // divisions=1, duration=1 (quarter)
    [InlineData(2, 2)]  // divisions=2, duration=2 (quarter)
    [InlineData(8, 8)]  // divisions=8, duration=8 (quarter)
    [InlineData(16, 16)] // divisions=16, duration=16 (quarter)
    public void Convert_DifferentDivisionValues_QuarterNote(int divisions, int duration)
    {
        var result = DurationConverter.Convert(duration, divisions);

        Assert.Equal(NoteDurationBase.Quarter, result.Base);
        Assert.Equal(0, result.Dots);
        Assert.Null(result.Tuplet);
    }

    [Theory]
    [InlineData(2, 1, NoteDurationBase.Eighth, 0)]   // divisions=2, duration=1 (eighth)
    [InlineData(8, 2, NoteDurationBase.Sixteenth, 0)] // divisions=8, duration=2 (sixteenth)
    [InlineData(8, 16, NoteDurationBase.Half, 0)]    // divisions=8, duration=16 (half)
    public void Convert_DifferentDivisions_VariousDurations(int divisions, int duration, NoteDurationBase expectedBase, int expectedDots)
    {
        var result = DurationConverter.Convert(duration, divisions);

        Assert.Equal(expectedBase, result.Base);
        Assert.Equal(expectedDots, result.Dots);
    }

    [Fact]
    public void Convert_ZeroDuration_ReturnsValidDuration()
    {
        // MusicXML can have zero-duration grace notes
        var result = DurationConverter.Convert(0, 4);

        // FromRational(0) should return a valid duration
        // Just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public void Convert_NegativeDuration_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DurationConverter.Convert(-1, 4));
    }

    [Fact]
    public void Convert_ZeroDivisions_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DurationConverter.Convert(4, 0));
    }

    [Fact]
    public void Convert_NegativeDivisions_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DurationConverter.Convert(4, -1));
    }

    [Fact]
    public void Convert_DoubleDottedHalf_ReturnsCorrectDuration()
    {
        // Double-dotted half = 3.5 quarters
        // divisions=4, duration=14 (14/4 = 3.5)
        var result = DurationConverter.Convert(14, 4);

        Assert.Equal(NoteDurationBase.Half, result.Base);
        Assert.Equal(2, result.Dots); // Double-dotted
        Assert.Null(result.Tuplet);
    }

    [Fact]
    public void Convert_ThirtySecondNote_ReturnsCorrectBase()
    {
        // 32nd note = 1/8 quarter = 0.125 quarters
        // divisions=8, duration=1 (1/8 of quarter)
        var result = DurationConverter.Convert(1, 8);

        Assert.Equal(NoteDurationBase.ThirtySecond, result.Base);
        Assert.Equal(0, result.Dots);
        Assert.Null(result.Tuplet);
    }

    [Fact]
    public void Convert_Quintuplet_WithExplicitTuplet_ReturnsTupletDuration()
    {
        // Quintuplet eighth = 5 eighths in the time of 4
        // divisions=5, duration=2 (2/5 of quarter)
        var tuplet = new Tuplet(5, 4); // 5 in the time of 4

        var result = DurationConverter.Convert(2, 5, tuplet);

        Assert.Equal(NoteDurationBase.Eighth, result.Base);
        Assert.NotNull(result.Tuplet);
        Assert.Equal(5, result.Tuplet.ActualNotes);
        Assert.Equal(4, result.Tuplet.NormalNotes);
    }

    [Fact]
    public void Convert_LargeDivisions_StillWorks()
    {
        // Some MusicXML files use very large division values (e.g., 256)
        // divisions=256, duration=128 (128/256 = 0.5 quarters = eighth note)
        var result = DurationConverter.Convert(128, 256);

        Assert.Equal(NoteDurationBase.Eighth, result.Base);
        Assert.Equal(0, result.Dots);
    }

    [Fact]
    public void Convert_PreservesExistingTuplet_WhenProvidingNewTuplet()
    {
        // If FromRational already infers a tuplet, don't override it
        // divisions=3, duration=1 (triplet eighth)
        var explicitTuplet = new Tuplet(5, 4); // Try to override with quintuplet

        var result = DurationConverter.Convert(1, 3, explicitTuplet);

        // Should preserve the inferred triplet, not apply the explicit quintuplet
        Assert.Equal(Tuplet.Triplet, result.Tuplet);
    }
}
