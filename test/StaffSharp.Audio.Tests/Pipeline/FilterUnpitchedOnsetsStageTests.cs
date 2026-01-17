using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;

namespace StaffSharp.Audio.Tests.Pipeline;

public class FilterUnpitchedOnsetsStageTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoUnpitchedOnsets_ReturnsAllOnsets()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var pitches = new int[] { 60, 62, 64, 65 };

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Equal(4, filteredOnsets.Length);
        Assert.Equal(4, filteredPitches.Length);
        // First onset should be shifted to 0
        Assert.Equal(0.0, filteredOnsets[0]);
        Assert.Equal(0.5, filteredOnsets[1]);
        Assert.Equal(1.0, filteredOnsets[2]);
        Assert.Equal(1.5, filteredOnsets[3]);
        Assert.Equal(new[] { 60, 62, 64, 65 }, filteredPitches);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnpitchedFirstOnset_FiltersAndShifts()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var pitches = new int[] { -1, 60, 62, 64 }; // First is unpitched

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Equal(3, filteredOnsets.Length);
        Assert.Equal(3, filteredPitches.Length);
        // Onsets should be shifted so first pitched note is at 0
        Assert.Equal(0.0, filteredOnsets[0]); // Was 0.5, now shifted to 0
        Assert.Equal(0.5, filteredOnsets[1]); // Was 1.0, now 0.5
        Assert.Equal(1.0, filteredOnsets[2]); // Was 1.5, now 1.0
        Assert.Equal(new[] { 60, 62, 64 }, filteredPitches);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleUnpitchedOnsets_FiltersAll()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5, 2.0 };
        var pitches = new int[] { -1, 60, -1, 62, 64 };

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Equal(3, filteredOnsets.Length);
        Assert.Equal(3, filteredPitches.Length);
        // Should keep onsets at 0.5, 1.5, 2.0, shifted by 0.5
        Assert.Equal(0.0, filteredOnsets[0]); // Was 0.5
        Assert.Equal(1.0, filteredOnsets[1]); // Was 1.5
        Assert.Equal(1.5, filteredOnsets[2]); // Was 2.0
        Assert.Equal(new[] { 60, 62, 64 }, filteredPitches);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllUnpitchedOnsets_ReturnsEmpty()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var pitches = new int[] { -1, -1, -1 };

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Empty(filteredOnsets);
        Assert.Empty(filteredPitches);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyInput_ReturnsEmpty()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = Array.Empty<double>();
        var pitches = Array.Empty<int>();

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Empty(filteredOnsets);
        Assert.Empty(filteredPitches);
    }

    [Fact]
    public async Task ExecuteAsync_WithMismatchedLengths_ThrowsArgumentException()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var pitches = new int[] { 60, 62 }; // Different length

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await stage.ExecuteAsync(onsets, pitches, default));
    }

    [Fact]
    public async Task ExecuteAsync_WithSinglePitchedOnset_ShiftsToZero()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 2.5 };
        var pitches = new int[] { 60 };

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Single(filteredOnsets);
        Assert.Single(filteredPitches);
        Assert.Equal(0.0, filteredOnsets[0]); // Should be shifted from 2.5 to 0.0
        Assert.Equal(60, filteredPitches[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnpitchedAtEnd_DoesNotAffectShift()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 0.5, 1.0, 1.5, 2.0 };
        var pitches = new int[] { 60, 62, 64, -1 }; // Last is unpitched

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Equal(3, filteredOnsets.Length);
        // Shift is based on first pitched onset (0.5)
        Assert.Equal(0.0, filteredOnsets[0]); // Was 0.5
        Assert.Equal(0.5, filteredOnsets[1]); // Was 1.0
        Assert.Equal(1.0, filteredOnsets[2]); // Was 1.5
        Assert.Equal(new[] { 60, 62, 64 }, filteredPitches);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesPitchValues()
    {
        // Arrange
        var stage = new FilterUnpitchedOnsetsStage(PipelineProgress.Null);
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var pitches = new int[] { -1, 127, 0 }; // Boundary values

        // Act
        var (filteredOnsets, filteredPitches) = await stage.ExecuteAsync(onsets, pitches, default);

        // Assert
        Assert.Equal(2, filteredPitches.Length);
        Assert.Equal(127, filteredPitches[0]); // Max MIDI value
        Assert.Equal(0, filteredPitches[1]);   // Min MIDI value
    }
}
