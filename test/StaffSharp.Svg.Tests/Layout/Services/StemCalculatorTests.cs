namespace StaffSharp.Svg.Tests.Layout.Services;

using StaffSharp.Notation;
using StaffSharp.Svg;
using StaffSharp.Svg.Layout;
using StaffSharp.Svg.Layout.Services;
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
        Assert.True(symbol.StemUp);
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
        Assert.False(symbol.StemUp);
    }

    [Fact]
    public void CalculateStem_Voice1_StemUp()
    {
        // Arrange
        var symbol = new NoteLayoutSymbol
        {
            Note = new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Quarter),
            VoiceNumber = 1,
            Y = 60.0
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.True(symbol.StemUp);
    }

    [Fact]
    public void CalculateStem_Voice2_StemDown()
    {
        // Arrange
        var symbol = new NoteLayoutSymbol
        {
            Note = new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Quarter),
            VoiceNumber = 2,
            Y = 60.0
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateStem(symbol, staffBaseline, _context);

        // Assert
        Assert.False(symbol.StemUp);
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
        Assert.True(symbol.StemUp);
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
        Assert.False(symbol.StemUp);
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
        var group = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 65.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 62.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.All(group, symbol => Assert.True(symbol.StemUp));
    }

    [Fact]
    public void CalculateBeamedGroupStems_AboveMiddle_StemsDown()
    {
        // Arrange
        var group = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 30.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 35.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 32.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.All(group, symbol => Assert.False(symbol.StemUp));
    }

    [Fact]
    public void CalculateBeamedGroupStems_AssignsBeamGroupId()
    {
        // Arrange
        var group = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 65.0, x: 20.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.NotNull(group[0].BeamGroupId);
        Assert.Equal(group[0].BeamGroupId, group[1].BeamGroupId);
    }

    [Fact]
    public void CalculateBeamedGroupStems_SetsFirstAndLastFlags()
    {
        // Arrange
        var group = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 65.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 62.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.True(group[0].IsFirstInBeamGroup);
        Assert.False(group[0].IsLastInBeamGroup);
        Assert.False(group[1].IsFirstInBeamGroup);
        Assert.False(group[1].IsLastInBeamGroup);
        Assert.False(group[2].IsFirstInBeamGroup);
        Assert.True(group[2].IsLastInBeamGroup);
    }

    [Fact]
    public void CalculateBeamedGroupStems_CalculatesBeamCount()
    {
        // Arrange
        var group = new List<LayoutSymbol>
        {
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Eighth, y: 60.0, x: 10.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth, y: 65.0, x: 20.0),
            LayoutTestHelpers.CreateNoteSymbol(duration: SymbolicDuration.Sixteenth, y: 62.0, x: 30.0)
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.Equal(1, group[0].BeamCount);
        Assert.Equal(2, group[1].BeamCount);
        Assert.Equal(2, group[2].BeamCount);
    }

    [Fact]
    public void CalculateBeamedGroupStems_Voice2_StemsDown()
    {
        // Arrange
        var group = new List<LayoutSymbol>
        {
            new NoteLayoutSymbol
            {
                Note = new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Eighth),
                VoiceNumber = 2,
                Y = 60.0,
                X = 10.0
            },
            new NoteLayoutSymbol
            {
                Note = new NotationNote(new Pitch(PitchClass.D, 4), SymbolicDuration.Eighth),
                VoiceNumber = 2,
                Y = 65.0,
                X = 20.0
            }
        };
        var staffBaseline = 50.0;

        // Act
        StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, _context);

        // Assert
        Assert.All(group, symbol => Assert.False(symbol.StemUp));
    }
}
