namespace StaffSharp.Svg.Tests.Layout.Services;

using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;
using StaffSharp.Notation;

using Xunit;

public class BoundsCalculatorTests
{
    private const double staffSpace = 10.0;

    #region CalculateSymbolBounds Tests

    [Fact]
    public void CalculateSymbolBounds_SimpleSymbol_ReturnsBasicBounds()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 4, SymbolicDuration.Whole, 10.0, 50.0, 5.0, 10.0);

        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(50.0, minY);
        Assert.Equal(60.0, maxY); // Y + staffSpace
    }

    [Fact]
    public void CalculateSymbolBounds_WithStemUp_IncludesStemInBounds()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 4, SymbolicDuration.Quarter, 10.0, 50.0, 5.0, 10.0);
        symbol.Stem = symbol.Stem with { Y1 = 50.0, Y2 = 20.0, Up = true };

        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(20.0, minY); // Minimum is stem top
        Assert.Equal(60.0, maxY); // Maximum includes notehead
    }

    [Fact]
    public void CalculateSymbolBounds_WithStemDown_IncludesStemInBounds()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 4, SymbolicDuration.Quarter, 10.0, 50.0, 5.0, 10.0);
        symbol.Stem = symbol.Stem with { Y1 = 50.0, Y2 = 80.0, Up = false };

        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(50.0, minY);
        Assert.Equal(80.0, maxY); // Stem extends below
    }

    [Fact]
    public void CalculateSymbolBounds_WithLedgerLinesAbove_ExtendsBoundsUpward()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 6, SymbolicDuration.Quarter, 10.0, 10.0, 5.0, 10.0);
        symbol.LedgerLineCount = 2;
        symbol.LedgerLinesAbove = true;

        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(-10.0, minY); // Y - (2 * staffSpace)
        Assert.Equal(20.0, maxY);
    }

    [Fact]
    public void CalculateSymbolBounds_WithLedgerLinesBelow_ExtendsBoundsDownward()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 2, SymbolicDuration.Quarter, 10.0, 100.0, 5.0, 10.0);
        symbol.LedgerLineCount = 3;
        symbol.LedgerLinesAbove = false;

        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(100.0, minY);
        Assert.Equal(130.0, maxY); // Y + (3 * staffSpace)
    }

    [Fact]
    public void CalculateSymbolBounds_ChordSymbol_IncludesAllNoteheads()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateChordSymbol(
            [
                new Pitch(PitchClass.C, 4),
                new Pitch(PitchClass.E, 4),
                new Pitch(PitchClass.G, 4)
            ],
            SymbolicDuration.Quarter);
        // Add notehead positions
        symbol.NoteheadYPositions.Add(50.0);  // C
        symbol.NoteheadYPositions.Add(40.0);  // E
        symbol.NoteheadYPositions.Add(30.0);  // G


        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(30.0, minY); // Highest notehead
        Assert.Equal(60.0, maxY); // Lowest notehead + staff space
    }

    [Fact]
    public void CalculateSymbolBounds_ChordWithStemAndLedgerLines_IncludesAll()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateChordSymbol(
            [
                new Pitch(PitchClass.C, 4),
                new Pitch(PitchClass.E, 4)
            ],
            SymbolicDuration.Quarter,
            y: 50.0);
        symbol.Stem = symbol.Stem with { Y1 = 50.0, Y2 = 20.0, Up = true };
        symbol.LedgerLineCount = 1;
        symbol.LedgerLinesAbove = false;
        symbol.NoteheadYPositions.Add(50.0);
        symbol.NoteheadYPositions.Add(45.0);


        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(20.0, minY); // Stem extends highest
        Assert.Equal(60.0, maxY); // Ledger line extends below (50 + 10)
    }

    [Fact]
    public void CalculateSymbolBounds_RestSymbol_ReturnsBasicBounds()
    {
        // Arrange
        var symbol = LayoutTestHelpers.CreateRestSymbol(SymbolicDuration.Quarter, x: 10.0, y: 40.0);


        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(40.0, minY);
        Assert.Equal(50.0, maxY); // Y + staffSpace
    }

    [Fact]
    public void CalculateSymbolBounds_ClefSymbol_ReturnsBasicBounds()
    {
        // Arrange
        var symbol = new ClefLayoutSymbol
        {
            Clef = Clef.Treble,
            Y = 0.0,
            X = 0.0,
            Width = 10.0,
            Height = 20.0
        };

        // Act
        var (minY, maxY) = BoundsCalculator.CalculateSymbolBounds(symbol, staffSpace);

        // Assert
        Assert.Equal(0.0, minY);
        Assert.Equal(10.0, maxY);
    }

    #endregion

    #region CalculateStaffBounds Tests

    [Fact]
    public void CalculateStaffBounds_EmptyStaff_ReturnsStaffLinesBounds()
    {
        // Arrange
        var staff = LayoutTestHelpers.CreateStaff();
        var staffY = 100.0;

        // Act
        var (minY, maxY, height) = BoundsCalculator.CalculateStaffBounds(staff, staffY, staffSpace);

        // Assert
        Assert.Equal(100.0, minY); // Staff Y
        Assert.Equal(140.0, maxY); // Staff Y + 4 * staffSpace (5 lines)
        Assert.Equal(40.0, height);
    }

    [Fact]
    public void CalculateStaffBounds_WithSymbols_IncludesSymbolExtents()
    {
        // Arrange
        var staff = LayoutTestHelpers.CreateStaff();
        var measure = LayoutTestHelpers.CreateMeasure();
        var symbol = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 6);
        symbol.Y = 0.0; // Relative to staff
        symbol.Stem = symbol.Stem with { Y1 = 0.0, Y2 = -30.0 }; // Extends above staff
        measure.Symbols.Add(symbol);
        staff.Measures.Add(measure);
        var staffY = 100.0;

        // Act
        var (minY, maxY, height) = BoundsCalculator.CalculateStaffBounds(staff, staffY, staffSpace);

        // Assert
        Assert.Equal(70.0, minY); // StaffY + symbol's stem top (-30)
        Assert.Equal(140.0, maxY); // Staff lines extent
        Assert.Equal(70.0, height);
    }

    [Fact]
    public void CalculateStaffBounds_WithLedgerLines_ExtendsBeyondStaff()
    {
        // Arrange
        var staff = LayoutTestHelpers.CreateStaff();
        var measure = LayoutTestHelpers.CreateMeasure();
        var highNote = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 6);
        highNote.Y = -20.0;
        highNote.LedgerLineCount = 2;
        highNote.LedgerLinesAbove = true;
        measure.Symbols.Add(highNote);
        staff.Measures.Add(measure);
        var staffY = 100.0;

        // Act
        var (minY, maxY, height) = BoundsCalculator.CalculateStaffBounds(staff, staffY, staffSpace);

        // Assert
        Assert.True(minY < 100.0); // Should extend above staff
        Assert.Equal(140.0, maxY);
    }

    [Fact]
    public void CalculateStaffBounds_MultipleSymbols_AccountsForAllBounds()
    {
        // Arrange
        var staff = LayoutTestHelpers.CreateStaff(width: 200.0);
        var measure = LayoutTestHelpers.CreateMeasure(width: 200.0);
        var highNote = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 6);
        highNote.Y = -10.0;
        highNote.X = 10.0;
        var lowNote = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 2);
        lowNote.Y = 60.0;
        lowNote.X = 50.0;
        measure.Symbols.Add(highNote);
        measure.Symbols.Add(lowNote);
        staff.Measures.Add(measure);
        var staffY = 100.0;

        // Act
        var (minY, maxY, height) = BoundsCalculator.CalculateStaffBounds(staff, staffY, staffSpace);

        // Assert
        Assert.Equal(90.0, minY);  // staffY + high note Y (-10)
        Assert.Equal(170.0, maxY); // staffY + low note Y (60) + staffSpace (10)
        Assert.Equal(80.0, height);
    }

    [Fact]
    public void CalculateStaffBounds_MultipleMeasures_ProcessesAllMeasures()
    {
        // Arrange
        var staff = LayoutTestHelpers.CreateStaff(width: 200.0);
        var measure1 = LayoutTestHelpers.CreateMeasure(width: 100.0);
        var measure2 = LayoutTestHelpers.CreateMeasure(x: 100.0, width: 100.0);
        var symbol1 = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 5, SymbolicDuration.Quarter, x: 10.0, y: 10.0);
        var symbol2 = LayoutTestHelpers.CreateNoteSymbol(PitchClass.C, 3, SymbolicDuration.Quarter, x: 110.0, y: 50.0);
        measure1.Symbols.Add(symbol1);
        measure2.Symbols.Add(symbol2);
        staff.Measures.Add(measure1);
        staff.Measures.Add(measure2);

        var staffY = 100.0;

        // Act
        var (minY, maxY, height) = BoundsCalculator.CalculateStaffBounds(staff, staffY, staffSpace);

        // Assert
        Assert.Equal(100.0, minY); // Staff Y (symbols don't extend above)
        Assert.Equal(160.0, maxY); // staffY + symbol2 Y (50) + staffSpace (10)
        Assert.Equal(60.0, height);
    }

    #endregion

    #region CalculateSystemHeight Tests

    [Fact]
    public void CalculateSystemHeight_EmptySystem_ReturnsZero()
    {
        // Arrange
        var system = LayoutTestHelpers.CreateSystem(height: 0.0);
        var interStaffSpacing = 20.0;

        // Act
        var height = BoundsCalculator.CalculateSystemHeight(system, interStaffSpacing);

        // Assert
        Assert.Equal(0, height);
    }

    [Fact]
    public void CalculateSystemHeight_SingleStaff_ReturnsStaffHeight()
    {
        // Arrange
        var system = LayoutTestHelpers.CreateSystem(height: 50.0);
        var staff = LayoutTestHelpers.CreateStaff(height: 50.0);
        system.AddStaff(staff);

        var interStaffSpacing = 20.0;

        // Act
        var height = BoundsCalculator.CalculateSystemHeight(system, interStaffSpacing);

        // Assert
        Assert.Equal(50.0, height);
    }

    [Fact]
    public void CalculateSystemHeight_TwoStaves_IncludesInterStaffSpacing()
    {
        // Arrange
        var system = LayoutTestHelpers.CreateSystem(height: 120.0);
        var staff1 = LayoutTestHelpers.CreateStaff(height: 40.0);
        var staff2 = LayoutTestHelpers.CreateStaff(y: 60.0, height: 40.0);
        system.AddStaff(staff1);
        system.AddStaff(staff2);

        var interStaffSpacing = 20.0;

        // Act
        var height = BoundsCalculator.CalculateSystemHeight(system, interStaffSpacing);

        // Assert
        Assert.Equal(100.0, height); // 40 + 20 + 40
    }

    [Fact]
    public void CalculateSystemHeight_ThreeStaves_IncludesTwoInterStaffSpacings()
    {
        // Arrange
        var system = LayoutTestHelpers.CreateSystem(height: 160.0);
        var staff1 = LayoutTestHelpers.CreateStaff(height: 40.0);
        var staff2 = LayoutTestHelpers.CreateStaff(y: 60.0, height: 40.0);
        var staff3 = LayoutTestHelpers.CreateStaff(y: 120.0, height: 40.0);
        system.AddStaff(staff1);
        system.AddStaff(staff2);
        system.AddStaff(staff3);

        var interStaffSpacing = 20.0;

        // Act
        var height = BoundsCalculator.CalculateSystemHeight(system, interStaffSpacing);

        // Assert
        Assert.Equal(160.0, height); // 40 + 20 + 40 + 20 + 40
    }

    [Fact]
    public void CalculateSystemHeight_DifferentStaffHeights_SumsCorrectly()
    {
        // Arrange
        var system = LayoutTestHelpers.CreateSystem(height: 130.0);
        var staff1 = LayoutTestHelpers.CreateStaff(height: 50.0);
        var staff2 = LayoutTestHelpers.CreateStaff(y: 80.0, height: 30.0);
        system.AddStaff(staff1);
        system.AddStaff(staff2);

        var interStaffSpacing = 30.0;

        // Act
        var height = BoundsCalculator.CalculateSystemHeight(system, interStaffSpacing);

        // Assert
        Assert.Equal(110.0, height); // 50 + 30 + 30
    }

    [Fact]
    public void CalculateSystemHeight_DifferentInterStaffSpacing_ScalesCorrectly()
    {
        // Arrange
        var system = LayoutTestHelpers.CreateSystem(height: 100.0);
        var staff1 = LayoutTestHelpers.CreateStaff(height: 40.0);
        var staff2 = LayoutTestHelpers.CreateStaff(y: 50.0, height: 40.0);
        system.AddStaff(staff1);
        system.AddStaff(staff2);

        var smallSpacing = 10.0;
        var largeSpacing = 40.0;

        // Act
        var heightSmall = BoundsCalculator.CalculateSystemHeight(system, smallSpacing);
        var heightLarge = BoundsCalculator.CalculateSystemHeight(system, largeSpacing);

        // Assert
        Assert.Equal(90.0, heightSmall);   // 40 + 10 + 40
        Assert.Equal(120.0, heightLarge);  // 40 + 40 + 40
    }

    #endregion
}
