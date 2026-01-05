namespace StaffSharp.Svg.Tests.Layout.Services;

using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;
using StaffSharp.Notation;
using StaffSharp.Svg.Tests.Layout;

using Xunit;

/// <summary>
/// Tests for BeamGrouper to verify correct beam grouping logic.
/// </summary>
public class BeamGrouperTests
{
    [Fact]
    public void GroupBeamableNotes_AllQuarterNotes_ReturnsEmptyGroups()
    {
        // Arrange
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Quarter),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Quarter),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Quarter),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Quarter)
        };

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols);

        // Assert
        Assert.Empty(groups);
    }

    [Fact]
    public void GroupBeamableNotes_QuarterNoteThen6EighthNotes_BreaksAtHalfMeasure()
    {
        // Arrange - Quarter note on beat 1, then 6 eighth notes in 4/4 time
        // Beat 1: Quarter note (1 beat)
        // Beat 2: 2 eighth notes (remaining first half of measure - group 1)
        // Beat2 3-4: 4 eighth notes (second half - group 2)
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Quarter),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
        };

        var timeSignature = TimeSignature.CommonTime; // 4/4

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols, timeSignature);

        // Assert - Should create 2 groups: 2 eighths + 4 eighths
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[0].Count);
        Assert.Equal(4, groups[1].Count);
    }

    [Fact]
    public void GroupBeamableNotes_FourEighthNotes_ReturnsOneGroup()
    {
        // Arrange
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth)
        };

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols);

        // Assert
        Assert.Single(groups);
        Assert.Equal(4, groups[0].Count);
    }

    [Fact]
    public void GroupBeamableNotes_EighthsWithQuarterInMiddle_ReturnsTwoGroups()
    {
        // Arrange
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Quarter),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth)
        };

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols);

        // Assert
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[0].Count);
        Assert.Equal(2, groups[1].Count);
    }

    [Fact]
    public void GroupBeamableNotes_DifferentVoices_SeparatesGroups()
    {
        // Arrange
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, duration: SymbolicDuration.Eighth, voice: 1),
            LayoutTestHelpers.CreateNoteSymbol(PitchClass.D, duration: SymbolicDuration.Eighth, voice: 1),
            LayoutTestHelpers.CreateNoteSymbol(PitchClass.E, duration: SymbolicDuration.Eighth, voice: 2),
            LayoutTestHelpers.CreateNoteSymbol(PitchClass.F, duration: SymbolicDuration.Eighth, voice: 2)
        };

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols);

        // Assert
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[0].Count);
        Assert.All(groups[0], s => Assert.Equal(1, s.VoiceNumber));
        Assert.Equal(2, groups[1].Count);
        Assert.All(groups[1], s => Assert.Equal(2, s.VoiceNumber));
    }

    [Fact]
    public void GroupBeamableNotes_SixteenthNotes_GroupsTogether()
    {
        // Arrange
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth)
        };

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols);

        // Assert
        Assert.Single(groups);
        Assert.Equal(4, groups[0].Count);
    }

    [Fact]
    public void GroupBeamableNotes_MixedEighthsAndSixteenths_GroupsTogether()
    {
        // Arrange
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth)
        };

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols);

        // Assert
        Assert.Single(groups);
        Assert.Equal(4, groups[0].Count);
    }

    [Fact]
    public void GroupBeamableNotes_SixEighthsIn4_4_BreaksAtHalfMeasure()
    {
        // Arrange - 6 eighth notes in 4/4 time
        // Expected: 2 groups at half-measure boundary (4 + 2)
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth)
        };

        var timeSignature = TimeSignature.CommonTime; // 4/4

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols, timeSignature);

        // Assert
        Assert.Equal(2, groups.Count);
        Assert.Equal(4, groups[0].Count);
        Assert.Equal(2, groups[1].Count);
    }

    [Fact]
    public void GroupBeamableNotes_EightEighthsIn4_4_BreaksAtHalfMeasure()
    {
        // Arrange - 8 eighth notes in 4/4 time
        // Expected: 2 groups of 4 at half-measure boundary
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth)
        };

        var timeSignature = TimeSignature.CommonTime; // 4/4

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols, timeSignature);

        // Assert
        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(4, g.Count));
    }

    [Fact]
    public void GroupBeamableNotes_SixEighthsIn3_4_BreaksOnBeats()
    {
        // Arrange - 6 eighth notes in 3/4 time
        // Expected: 3 groups of 2 (one group per beat)
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth)
        };

        var timeSignature = new TimeSignature(3, 4); // 3/4

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols, timeSignature);

        // Assert
        Assert.Equal(3, groups.Count);
        Assert.All(groups, g => Assert.Equal(2, g.Count));
    }

    [Fact]
    public void GroupBeamableNotes_SixEighthsIn6_8_GroupsByDottedQuarter()
    {
        // Arrange - 6 eighth notes in 6/8 time (compound meter)
        // Expected: 2 groups of 3 (one per dotted quarter beat)
        var symbols = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth)
        };

        var timeSignature = new TimeSignature(6, 8); // 6/8

        // Act
        var groups = BeamGrouper.GroupBeamableNotes(symbols, timeSignature);

        // Assert
        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(3, g.Count));
    }
}
