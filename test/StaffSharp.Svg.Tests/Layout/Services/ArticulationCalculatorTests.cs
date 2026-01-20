namespace StaffSharp.Svg.Tests.Layout.Services;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;
using StaffSharp.Notation;

using Xunit;

/// <summary>
/// Tests for ArticulationCalculator to verify correct articulation positioning.
/// </summary>
public class ArticulationCalculatorTests
{
    private readonly SvgContext _context = new() { StaffSpace = 10 };

    [Fact]
    public void CalculateArticulations_NoDecorations_ReturnsEmptyList()
    {
        // Arrange
        var decorations = Array.Empty<Decoration>();
        var note = CreateNoteSymbol(50.0, 30.0, stemUp: true);

        // Act
        var result = ArticulationCalculator.CalculateArticulations(
            note,
            decorations,
            _context);

        // Assert
        Assert.Empty(result);
    }

    private static NoteLayoutSymbol CreateNoteSymbol(double x, double y, bool stemUp, double stemLength = 35.0)
    {
        var stemY1 = y;
        var stemY2 = stemUp ? y - stemLength : y + stemLength;
        var stemX = stemUp ? x + 11 : x + 1;

        return new NoteLayoutSymbol
        {
            Note = new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Quarter, Velocity.MezzoForte),
            Bounds = new(x, y, 0, 0),
            Stem = new StemInfo(stemX, stemY1, stemY2, stemUp),
            Beam = BeamInfo.None
        };
    }

    [Fact]
    public void CalculateArticulations_SingleStaccato_StemUp_PlacesBelowNote()
    {
        // Arrange
        var decorations = new[] { Decoration.Staccato };
        var note = CreateNoteSymbol(50.0, 30.0, stemUp: true);

        // Act
        var result = ArticulationCalculator.CalculateArticulations(
            note,
            decorations,
            _context);

        // Assert
        Assert.Single(result);
        Assert.Equal(Decoration.Staccato, result[0].Type);
        Assert.Equal(50.0, result[0].X);
        // When stem is up, articulations on opposite side should be below stem endpoint
        Assert.True(result[0].Y > note.Stem.Y2, "Staccato should be below stem endpoint when stem is up");
    }

    [Fact]
    public void CalculateArticulations_SingleStaccato_StemDown_PlacesAboveNote()
    {
        // Arrange
        var decorations = new[] { Decoration.Staccato };
        var note = CreateNoteSymbol(50.0, 30.0, stemUp: false);

        // Act
        var result = ArticulationCalculator.CalculateArticulations(
            note,
            decorations,
            _context);

        // Assert
        Assert.Single(result);
        Assert.Equal(Decoration.Staccato, result[0].Type);
        Assert.Equal(50.0, result[0].X);
        // When stem is down, articulations on opposite side should be above stem endpoint
        Assert.True(result[0].Y < note.Stem.Y2, "Staccato should be above stem endpoint when stem is down");
    }

    [Fact]
    public void CalculateArticulations_Fermata_AlwaysPlacedAbove()
    {
        // Arrange - Fermata should always be above, regardless of stem direction
        var decorations = new[] { Decoration.Fermata };
        var noteStemUp = CreateNoteSymbol(50.0, 30.0, stemUp: true);
        var noteStemDown = CreateNoteSymbol(50.0, 30.0, stemUp: false);

        // Act - Stem up
        var resultStemUp = ArticulationCalculator.CalculateArticulations(
            noteStemUp,
            decorations,
            _context);

        // Act - Stem down
        var resultStemDown = ArticulationCalculator.CalculateArticulations(
            noteStemDown,
            decorations,
            _context);

        // Assert
        Assert.Single(resultStemUp);
        // Fermata always goes above - for stem up, above is at the notehead
        Assert.True(resultStemUp[0].Y < noteStemUp.Bounds.Y, "Fermata should be above note when stem is up");

        Assert.Single(resultStemDown);
        // Fermata always goes above - for stem down, above is at the stem endpoint
        Assert.True(resultStemDown[0].Y < noteStemDown.Stem.Y2, "Fermata should be above stem endpoint when stem is down");
    }

    [Fact]
    public void CalculateArticulations_MultipleArticulations_StacksInCorrectOrder()
    {
        // Arrange - Staccato should be closest to notehead, then accent
        var decorations = new[] { Decoration.Accent, Decoration.Staccato };
        var note = CreateNoteSymbol(50.0, 30.0, stemUp: true);

        // Act
        var result = ArticulationCalculator.CalculateArticulations(
            note,
            decorations,
            _context);

        // Assert
        Assert.Equal(2, result.Count);

        // First item should be staccato (priority 1, closer to stem endpoint)
        Assert.Equal(Decoration.Staccato, result[0].Type);

        // Second item should be accent (priority 2, farther from stem endpoint)
        Assert.Equal(Decoration.Accent, result[1].Type);

        // Both should be below stem endpoint for stem up
        Assert.True(result[0].Y > note.Stem.Y2, "Staccato should be below stem endpoint");
        Assert.True(result[1].Y > note.Stem.Y2, "Accent should be below stem endpoint");
        // Accent should be farther from stem endpoint than staccato
        Assert.True(result[1].Y > result[0].Y, "Accent should be stacked farther from stem endpoint than staccato");
    }

    [Fact]
    public void CalculateArticulations_StaccatoAndFermata_FermataPlacedAboveStaccatoBelow()
    {
        // Arrange - When stem is up, staccato goes below (opposite stem), fermata goes above (always above)
        var decorations = new[] { Decoration.Staccato, Decoration.Fermata };
        var note = CreateNoteSymbol(50.0, 30.0, stemUp: true);

        // Act
        var result = ArticulationCalculator.CalculateArticulations(
            note,
            decorations,
            _context);

        // Assert
        Assert.Equal(2, result.Count);

        var staccato = result.Single(r => r.Type == Decoration.Staccato);
        var fermata = result.Single(r => r.Type == Decoration.Fermata);

        // Staccato should be below stem endpoint (stem up)
        Assert.True(staccato.Y > note.Stem.Y2, "Staccato should be below stem endpoint when stem is up");
        // Fermata should be above notehead (stem up)
        Assert.True(fermata.Y < note.Bounds.Y, "Fermata should be above note when stem is up");
    }
}
