namespace StaffSharp.Svg.Tests.Layout.Services;

using System.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;
using StaffSharp.Notation;
using StaffSharp.Svg.Tests.Layout;

using Xunit;

/// <summary>
/// Tests for StemCalculator to verify correct stem direction and positioning logic.
/// </summary>
public class StemCalculatorTests
{
    private const int StaffSpace = 10;
    private readonly SvgContext _context = new() { StaffSpace = StaffSpace, MaxWidth = 800 };

    [Fact]
    public void CalculateStem_NoteBelowMiddleLine_StemUp()
    {
        // Arrange - C4 is below the middle line in treble clef
        var symbol = LayoutTestHelpers.CreateNoteSymbol(
            pitchClass: PitchClass.C,
            octave: 4,
            duration: SymbolicDuration.Quarter,
            y: 60.0 // Below baseline
        );
        var staffBaseline = 50.0; // Middle line

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.True(symbol.Stem.Up);
    }

    [Fact]
    public void CalculateStem_NoteAboveMiddleLine_StemDown()
    {
        // Arrange - G4 is above the middle line in treble clef
        var symbol = LayoutTestHelpers.CreateNoteSymbol(
            pitchClass: PitchClass.G,
            octave: 4,
            duration: SymbolicDuration.Quarter,
            y: 40.0 // Above baseline
        );
        var staffBaseline = 50.0; // Middle line

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.False(symbol.Stem.Up);
    }

    [Fact]
    public void CalculateStem_Voice1_StemUp()
    {
        // Arrange
        var symbol = new NoteLayoutSymbol
        {
            Note = new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Quarter),
            VoiceNumber = 1,
            Bounds = new Bounds(0, 60, 0, 0),
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.True(symbol.Stem.Up);
    }

    [Fact]
    public void CalculateStem_Voice2_StemDown()
    {
        // Arrange
        var symbol = new NoteLayoutSymbol
        {
            Note = new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Quarter),
            VoiceNumber = 2,
            Bounds = new Bounds(0, 60, 0, 0),
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.False(symbol.Stem.Up);
    }

    [Fact]
    public void CalculateStem_ChordBelowMiddle_StemUp()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateChordSymbol(
            notes: [new Pitch(PitchClass.C, 4), new Pitch(PitchClass.E, 4), new Pitch(PitchClass.G, 4)],
            duration: SymbolicDuration.Quarter,
            y: 60.0
        );
        symbol.NoteheadYPositions.Add(60.0);
        symbol.NoteheadYPositions.Add(55.0);
        symbol.NoteheadYPositions.Add(50.0);
        var staffBaseline = 45.0;

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.True(symbol.Stem.Up);
    }

    [Fact]
    public void CalculateStem_ChordAboveMiddle_StemDown()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateChordSymbol(
            notes: [new Pitch(PitchClass.C, 5), new Pitch(PitchClass.E, 5), new Pitch(PitchClass.G, 5)],
            duration: SymbolicDuration.Quarter,
            y: 30.0
        );
        symbol.NoteheadYPositions.Add(30.0);
        symbol.NoteheadYPositions.Add(25.0);
        symbol.NoteheadYPositions.Add(20.0);
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.False(symbol.Stem.Up);
    }

    [Fact]
    public void RequiresStem_QuarterNote_ReturnsTrue()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Quarter);

        // Act & Assert
        Assert.True(StemCalculator.RequiresStem(symbol));
    }

    [Fact]
    public void RequiresStem_WholeNote_ReturnsFalse()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Whole);

        // Act & Assert
        Assert.False(StemCalculator.RequiresStem(symbol));
    }

    [Fact]
    public void RequiresStem_HalfNote_ReturnsTrue()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Half);

        // Act & Assert
        Assert.True(StemCalculator.RequiresStem(symbol));
    }

    [Fact]
    public void CalculateBeamedGroupStems_BelowMiddle_StemsUp()
    {
        // Arrange
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 65.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 62.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.All(group, symbol => Assert.True(symbol.Stem.Up));
    }

    [Fact]
    public void CalculateBeamedGroupStems_AboveMiddle_StemsDown()
    {
        // Arrange
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 30.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 35.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 32.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.All(group, symbol => Assert.False(symbol.Stem.Up));
    }

    [Fact]
    public void CalculateBeamedGroupStems_AssignsBeamGroupId()
    {
        // Arrange
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 65.0, x: 20.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.NotNull(group[0].Beam.GroupId);
        Assert.Equal(group[0].Beam.GroupId, group[1].Beam.GroupId);
    }

    [Fact]
    public void CalculateBeamedGroupStems_SetsFirstAndLastFlags()
    {
        // Arrange
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 65.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 62.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.True(group[0].Beam.IsFirstInGroup);
        Assert.False(group[0].Beam.IsLastInGroup);
        Assert.False(group[1].Beam.IsFirstInGroup);
        Assert.False(group[1].Beam.IsLastInGroup);
        Assert.False(group[2].Beam.IsFirstInGroup);
        Assert.True(group[2].Beam.IsLastInGroup);
    }

    [Fact]
    public void CalculateBeamedGroupStems_CalculatesBeamCount()
    {
        // Arrange
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth, y: 65.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth, y: 62.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.Equal(1, group[0].Beam.BeamCount);
        Assert.Equal(2, group[1].Beam.BeamCount);
        Assert.Equal(2, group[2].Beam.BeamCount);
    }

    [Fact]
    public void CalculateBeamedGroupStems_Voice2_StemsDown()
    {
        // Arrange
        var group = new List<NoteLayoutSymbol>
        {
            new NoteLayoutSymbol
            {
                Note = new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Eighth),
                VoiceNumber = 2,
                Bounds = new Bounds(10, 60, 0, 0)
            },
            new NoteLayoutSymbol
            {
                Note = new NotationNote(new Pitch(PitchClass.D, 4), SymbolicDuration.Eighth),
                VoiceNumber = 2,
                Bounds = new Bounds(20, 65, 0, 0),
            }
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.All(group, symbol => Assert.False(symbol.Stem.Up));
    }

    [Fact]
    public void CalculateBeamedGroupStems_SteepSlope_LimitsBeamAngle()
    {
        // Arrange - notes with large pitch difference that would create steep beam
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 70.0, x: 10.0),  // Low note
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 30.0, x: 50.0)   // High note
        };

        // Set Stem.Y1 to simulate notehead positions
        group[0].Stem = group[0].Stem with { Y1 = 70.0 };
        group[1].Stem = group[1].Stem with { Y1 = 30.0 };

        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert - beam slope should be limited to max 1 staff space
        var beamSlope = (group[1].Stem.Y2 - group[0].Stem.Y2) / (group[1].Stem.X - group[0].Stem.X);
        var maxSlopeInPixels = 1.0 * StaffSpace; // MaxBeamSlopeInSpaces = 1.0
        var beamWidth = group[1].Stem.X - group[0].Stem.X;
        var maxSlope = maxSlopeInPixels / beamWidth;

        Assert.True(Math.Abs(beamSlope) <= maxSlope + 0.01, // Small tolerance for floating point
            $"Beam slope {Math.Abs(beamSlope)} should not exceed max slope {maxSlope}");
    }

    [Fact]
    public void CalculateBeamedGroupStems_AllNotesHaveMinimumStemLength()
    {
        // Arrange - beamed group with notes at different pitches
        // This tests that middle notes also get minimum stem length, not just endpoints
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 50.0, x: 10.0),  // C
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 45.0, x: 30.0),  // D (higher)
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 40.0, x: 50.0),  // E (even higher)
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 50.0, x: 70.0)   // C (back down)
        };

        // Set Stem.Y1 to simulate notehead positions
        for (int i = 0; i < group.Count; i++)
        {
            group[i].Stem = group[i].Stem with { Y1 = group[i].Bounds.Y };
        }

        var staffBaseline = 50.0;
        var minStemLength = 3.5 * StaffSpace; // StemLength constant

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert - all notes should have at least minimum stem length
        AssertNotesHaveMinimumStemLength(group, minStemLength);
    }

    [Fact]
    public void CalculateBeamedGroupStems_ArchingMelody_MaintainsMinStemLengthForMiddleNotes()
    {
        // Arrange - arching melody pattern (low-high-low) where middle note might have short stem
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),  // Low
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 50.0, x: 30.0),  // Middle (higher)
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 55.0, x: 50.0),  // Middle-high
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 70.0)   // Low again
        };

        // Set Stem.Y1 to simulate notehead positions
        for (int i = 0; i < group.Count; i++)
        {
            group[i].Stem = group[i].Stem with { Y1 = group[i].Bounds.Y };
        }

        var staffBaseline = 50.0;
        var minStemLength = 3.5 * StaffSpace;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert - the middle notes (especially the highest one) should still have minimum stem length
        AssertNotesHaveMinimumStemLength(group, minStemLength);
    }

    [Fact]
    public void CalculateBeamedGroupStems_DippingMelody_MaintainsMinStemLengthForMiddleNotes()
    {
        // Arrange - dipping melody pattern (high-low-high) where middle note might have short stem
        var group = new List<NoteLayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 40.0, x: 10.0),  // High
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 50.0, x: 30.0),  // Middle (lower)
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 45.0, x: 50.0),  // Middle-low
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 40.0, x: 70.0)   // High again
        };

        // Set Stem.Y1 to simulate notehead positions
        for (int i = 0; i < group.Count; i++)
        {
            group[i].Stem = group[i].Stem with { Y1 = group[i].Bounds.Y };
        }

        var staffBaseline = 50.0;
        var minStemLength = 3.5 * StaffSpace;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert - the middle notes (especially the lowest one) should still have minimum stem length
        AssertNotesHaveMinimumStemLength(group, minStemLength);
    }

    private static void AssertNotesHaveMinimumStemLength(List<NoteLayoutSymbol> group, double minStemLength)
    {
        foreach (var symbol in group)
        {
            var actualStemLength = Math.Abs(symbol.Stem.Y2 - symbol.Stem.Y1);
            Assert.True(actualStemLength >= minStemLength - 0.01,
                $"Note at Y={symbol.Bounds.Y} has stem length {actualStemLength}, expected at least {minStemLength}");
        }
    }
}
