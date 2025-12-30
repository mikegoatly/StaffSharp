namespace StaffSharp.Core.Tests.Notation;

using StaffSharp.Notation;

public class DirectionTests
{
    [Fact]
    public void Direction_WithTempoAndBpm_CreatesCorrectly()
    {
        var direction = new Direction(
            DirectionType.Tempo,
            Placement.Above,
            "Allegro",
            120);

        Assert.Equal(DirectionType.Tempo, direction.Type);
        Assert.Equal(Placement.Above, direction.Placement);
        Assert.Equal("Allegro", direction.Content);
        Assert.Equal(120, direction.Bpm);
    }

    [Fact]
    public void Direction_WithDynamic_CreatesCorrectly()
    {
        var direction = new Direction(
            DirectionType.Dynamic,
            Placement.Below,
            "mf");

        Assert.Equal(DirectionType.Dynamic, direction.Type);
        Assert.Equal(Placement.Below, direction.Placement);
        Assert.Equal("mf", direction.Content);
        Assert.Null(direction.Bpm);
    }

    [Fact]
    public void Direction_WithRehearsalMark_CreatesCorrectly()
    {
        var direction = new Direction(
            DirectionType.RehearsalMark,
            Placement.Above,
            "A");

        Assert.Equal(DirectionType.RehearsalMark, direction.Type);
        Assert.Equal(Placement.Above, direction.Placement);
        Assert.Equal("A", direction.Content);
        Assert.Null(direction.Bpm);
    }

    [Fact]
    public void Direction_WithText_CreatesCorrectly()
    {
        var direction = new Direction(
            DirectionType.Text,
            Placement.Above,
            "D.C. al Coda");

        Assert.Equal(DirectionType.Text, direction.Type);
        Assert.Equal(Placement.Above, direction.Placement);
        Assert.Equal("D.C. al Coda", direction.Content);
        Assert.Null(direction.Bpm);
    }

    [Fact]
    public void Direction_WithCrescendo_CreatesCorrectly()
    {
        var direction = new Direction(
            DirectionType.Crescendo,
            Placement.Below,
            "cresc.");

        Assert.Equal(DirectionType.Crescendo, direction.Type);
        Assert.Equal(Placement.Below, direction.Placement);
        Assert.Equal("cresc.", direction.Content);
    }

    [Fact]
    public void Direction_WithDiminuendo_CreatesCorrectly()
    {
        var direction = new Direction(
            DirectionType.Diminuendo,
            Placement.Below,
            "dim.");

        Assert.Equal(DirectionType.Diminuendo, direction.Type);
        Assert.Equal(Placement.Below, direction.Placement);
        Assert.Equal("dim.", direction.Content);
    }

    [Fact]
    public void Direction_RecordEquality_WorksCorrectly()
    {
        var direction1 = new Direction(
            DirectionType.Tempo,
            Placement.Above,
            "Allegro",
            120);

        var direction2 = new Direction(
            DirectionType.Tempo,
            Placement.Above,
            "Allegro",
            120);

        var direction3 = new Direction(
            DirectionType.Tempo,
            Placement.Above,
            "Andante",
            80);

        Assert.Equal(direction1, direction2);
        Assert.NotEqual(direction1, direction3);
    }

    [Fact]
    public void Direction_WithInit_CanModifyProperties()
    {
        var direction = new Direction(
            DirectionType.Tempo,
            Placement.Above,
            "Allegro",
            120)
        {
            Content = "Allegro vivace"
        };

        Assert.Equal("Allegro vivace", direction.Content);
        Assert.Equal(120, direction.Bpm);
    }
}
