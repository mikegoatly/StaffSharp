namespace StaffSharp.Svg.Tests.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Passes;
using StaffSharp.Notation;
using StaffSharp.Svg.Tests.Layout;

using Xunit;

/// <summary>
/// Tests for SystemBreakingPass to verify correct system breaking logic.
/// </summary>
public class SystemBreakingPassTests
{
    [Fact]
    public void Run_WithMultipleStavesOfDifferentWidths_ShouldBreakAtSamePoints()
    {
        // Arrange: Create a piano score with treble and bass staves
        // Treble staff has sparse notes (wider measures)
        // Bass staff has dense notes (narrower measures but more content)
        
        var context = new SvgContext
        {
            MaxWidth = 400,
            Margins = new Margins(20, 20, 20, 20),
            StaffSpace = 10,
            Scale = 1.0
        };

        var metadata = new ScoreMetadata(
            Title: "Test Piano Score",
            Composer: "Test Composer",
            KeySignature: KeySignature.C,
            TimeSignature: TimeSignature.CommonTime,
            Tempo: 120
        );

        var model = new LayoutModel { Metadata = metadata };
        var system = new LayoutSystem();

        // Create treble staff with 8 measures - each is 80 units wide
        // This should fit about 4 measures per system (320 units + margins)
        var trebleStaff = CreateStaffWithMeasures(
            clef: Clef.Treble,
            measureCount: 8,
            measureWidth: 80.0
        );

        // Create bass staff with 8 measures - each is 100 units wide (denser)
        // This should fit about 3 measures per system (300 units + margins)
        var bassStaff = CreateStaffWithMeasures(
            clef: Clef.Bass,
            measureCount: 8,
            measureWidth: 100.0
        );

        system.AddStaff(trebleStaff);
        system.AddStaff(bassStaff);
        model.AddSystem(system);

        // Act
        var pass = new SystemBreakingPass();
        pass.Run(model, context);

        // Assert: Both staves should break at the same measure boundaries
        // This means each system should have the same number of staves
        Assert.True(model.Systems.Count > 1, "Should have created multiple systems");

        foreach (var resultSystem in model.Systems)
        {
            Assert.Equal(2, resultSystem.Staves.Count);
        }

        // Additional assertion: All systems should have measures spanning the same range
        // Get the first measure number in each system for each staff
        var trebleMeasureCounts = model.Systems
            .Select(s => s.Staves[0].Measures.Count)
            .ToList();
        
        var bassMeasureCounts = model.Systems
            .Select(s => s.Staves[1].Measures.Count)
            .ToList();

        for (int i = 0; i < model.Systems.Count; i++)
        {
            Assert.Equal(trebleMeasureCounts[i], bassMeasureCounts[i]);
        }
    }

    [Fact]
    public void Run_WithSingleStaff_ShouldBreakCorrectly()
    {
        // Arrange
        var context = new SvgContext
        {
            MaxWidth = 400,
            Margins = new Margins(20, 20, 20, 20),
            StaffSpace = 10,
            Scale = 1.0
        };

        var metadata = new ScoreMetadata(
            Title: "Single Staff Test",
            Composer: "Test",
            KeySignature: KeySignature.C,
            TimeSignature: TimeSignature.CommonTime,
            Tempo: 120
        );

        var model = new LayoutModel { Metadata = metadata };
        var system = new LayoutSystem();

        var staff = CreateStaffWithMeasures(
            clef: Clef.Treble,
            measureCount: 10,
            measureWidth: 90.0
        );

        system.AddStaff(staff);
        model.AddSystem(system);

        // Act
        var pass = new SystemBreakingPass();
        pass.Run(model, context);

        // Assert
        Assert.True(model.Systems.Count > 1, "Should have created multiple systems");
        
        // Verify all systems have exactly one staff
        foreach (var resultSystem in model.Systems)
        {
            Assert.Single(resultSystem.Staves);
        }

        // Verify total measure count is preserved
        var totalMeasures = model.Systems.Sum(s => s.Staves[0].Measures.Count);
        Assert.Equal(10, totalMeasures);
    }

    [Fact]
    public void Run_WithEmptySystems_ShouldHandleGracefully()
    {
        // Arrange
        var context = new SvgContext
        {
            MaxWidth = 800,
            Margins = new Margins(20, 20, 20, 20),
            StaffSpace = 10,
            Scale = 1.0
        };

        var model = new LayoutModel();

        // Act
        var pass = new SystemBreakingPass();
        pass.Run(model, context);

        // Assert
        Assert.Empty(model.Systems);
    }

    [Fact]
    public void Run_WithThreeStavesOfVaryingWidths_ShouldBreakAtSamePoints()
    {
        // Arrange: Simulate a complex score with 3 staves (e.g., organ with pedals)
        var context = new SvgContext
        {
            MaxWidth = 500,
            Margins = new Margins(20, 20, 20, 20),
            StaffSpace = 10,
            Scale = 1.0
        };

        var metadata = new ScoreMetadata(
            Title: "Three Staff Test",
            Composer: "Test",
            KeySignature: KeySignature.C,
            TimeSignature: TimeSignature.CommonTime,
            Tempo: 120
        );

        var model = new LayoutModel { Metadata = metadata };
        var system = new LayoutSystem();

        // Staff 1: 70 units per measure
        var staff1 = CreateStaffWithMeasures(Clef.Treble, 12, 70.0);
        
        // Staff 2: 90 units per measure  
        var staff2 = CreateStaffWithMeasures(Clef.Bass, 12, 90.0);
        
        // Staff 3: 110 units per measure (densest)
        var staff3 = CreateStaffWithMeasures(Clef.Bass, 12, 110.0);

        system.AddStaff(staff1);
        system.AddStaff(staff2);
        system.AddStaff(staff3);
        model.AddSystem(system);

        // Act
        var pass = new SystemBreakingPass();
        pass.Run(model, context);

        // Assert: All systems should have exactly 3 staves
        Assert.True(model.Systems.Count > 1, "Should have created multiple systems");

        foreach (var resultSystem in model.Systems)
        {
            Assert.Equal(3, resultSystem.Staves.Count);
        }

        // Verify all staves in each system have the same measure count
        foreach (var resultSystem in model.Systems)
        {
            var measureCounts = resultSystem.Staves.Select(s => s.Measures.Count).ToList();
            Assert.True(measureCounts.All(c => c == measureCounts[0]),
                $"All staves in a system should have the same measure count. Found: {string.Join(", ", measureCounts)}");
        }
    }

    /// <summary>
    /// Helper method to create a staff with a specified number of measures.
    /// </summary>
    private static LayoutStaff CreateStaffWithMeasures(Clef clef, int measureCount, double measureWidth)
    {
        var staff = new LayoutStaff
        {
            CurrentClef = clef,
            CurrentKeySignature = KeySignature.C
        };

        for (int i = 0; i < measureCount; i++)
        {
            var measure = new LayoutMeasure
            {
                Width = measureWidth,
                Height = 40.0,
                TimeSignature = TimeSignature.CommonTime
            };

            // Add a dummy note symbol to make it realistic
            var noteSymbol = LayoutTestHelpers.CreateNoteSymbol(
                pitchClass: PitchClass.C,
                octave: 4,
                duration: SymbolicDuration.Quarter,
                width: measureWidth - 10.0,
                voice: 1
            );

            measure.AddSymbol(noteSymbol);
            staff.AddMeasure(measure);
        }

        return staff;
    }
}
