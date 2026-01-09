namespace StaffSharp.Core.Tests.Notation;

using StaffSharp.Notation;

public class MeasureTests
{
    [Fact]
    public void Measure_WithDirections_StoresCorrectly()
    {
        var directions = new List<Direction>
        {
            new(DirectionType.Tempo, Placement.Above, "Allegro", 120),
            new(DirectionType.Dynamic, Placement.Below, "mf")
        };

        var measure = new Measure(
            1,
            [new Rest(SymbolicDuration.Quarter)],
            directions: directions);

        Assert.Equal(2, measure.Directions.Count);
        Assert.Equal(DirectionType.Tempo, measure.Directions[0].Type);
        Assert.Equal("Allegro", measure.Directions[0].Content);
        Assert.Equal(120, measure.Directions[0].Bpm);
        Assert.Equal(DirectionType.Dynamic, measure.Directions[1].Type);
        Assert.Equal("mf", measure.Directions[1].Content);
    }

    [Fact]
    public void Measure_WithoutDirections_HasEmptyList()
    {
        var measure = new Measure(
            1,
            [new Rest(SymbolicDuration.Quarter)]);

        Assert.Empty(measure.Directions);
    }

    [Fact]
    public void Measure_WithBarlineTypes_StoresCorrectly()
    {
        var measure = new Measure(
            1,
            [new Rest(SymbolicDuration.Quarter)],
            startBarline: BarlineType.RepeatStart,
            endBarline: BarlineType.RepeatEnd);

        Assert.Equal(BarlineType.RepeatStart, measure.StartBarline);
        Assert.Equal(BarlineType.RepeatEnd, measure.EndBarline);
    }

    [Fact]
    public void Measure_WithoutBarlineTypes_HasNullBarlines()
    {
        var measure = new Measure(
            1,
            [new Rest(SymbolicDuration.Quarter)]);

        Assert.Null(measure.StartBarline);
        Assert.Null(measure.EndBarline);
    }

    [Fact]
    public void Measure_WithAllOptionalParameters_CreatesCorrectly()
    {
        var directions = new List<Direction>
        {
            new(DirectionType.RehearsalMark, Placement.Above, "A")
        };

        var lyrics = new List<Lyric>
        {
            new([new LyricSyllable("Test", LyricSyllableType.Standalone)])
        };

        var repeatVariants = new List<int> { 1, 2 };

        var measure = new Measure(
            number: 5,
            events: [new Rest(SymbolicDuration.Quarter)],
            timeSignature: new TimeSignature(3, 4),
            repeatVariants: repeatVariants,
            lyrics: lyrics,
            startBarline: BarlineType.RepeatStart,
            endBarline: BarlineType.DoubleBar,
            directions: directions);

        Assert.Equal(5, measure.Number);
        Assert.Equal(new TimeSignature(3, 4), measure.TimeSignature);
        Assert.Equal(2, measure.RepeatVariants.Count);
        Assert.Single(measure.Lyrics);
        Assert.Equal(BarlineType.RepeatStart, measure.StartBarline);
        Assert.Equal(BarlineType.DoubleBar, measure.EndBarline);
        Assert.Single(measure.Directions);
        Assert.Equal("A", measure.Directions[0].Content);
    }

    [Fact]
    public void Measure_WithMultipleDirectionTypes_StoresAll()
    {
        var directions = new List<Direction>
        {
            new(DirectionType.Tempo, Placement.Above, "Andante", 80),
            new(DirectionType.Dynamic, Placement.Below, "p"),
            new(DirectionType.Crescendo, Placement.Below, "cresc."),
            new(DirectionType.RehearsalMark, Placement.Above, "B"),
            new(DirectionType.Text, Placement.Above, "Fine")
        };

        var measure = new Measure(
            1,
            [new Rest(SymbolicDuration.Whole)],
            directions: directions);

        Assert.Equal(5, measure.Directions.Count);
        Assert.Equal(DirectionType.Tempo, measure.Directions[0].Type);
        Assert.Equal(DirectionType.Dynamic, measure.Directions[1].Type);
        Assert.Equal(DirectionType.Crescendo, measure.Directions[2].Type);
        Assert.Equal(DirectionType.RehearsalMark, measure.Directions[3].Type);
        Assert.Equal(DirectionType.Text, measure.Directions[4].Type);
    }
}
