namespace StaffSharp.MachineLearning.Tests.ML.PostProcessing;

using StaffSharp.MachineLearning.ML.PostProcessing;
using StaffSharp.MachineLearning.Options;

public sealed class HarmonicSuppressorTests
{
    [Fact]
    public void SuppressHarmonics_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var suppressor = new HarmonicSuppressor();
        var notes = new List<NoteEvent>();

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SuppressHarmonics_SingleNote_ReturnsSingleNote()
    {
        // Arrange
        var suppressor = new HarmonicSuppressor();
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Single(result);
        Assert.Equal(60, result[0].Pitch.MidiNumber);
    }

    [Fact]
    public void SuppressHarmonics_OctaveHarmonic_QuieterHarmonicRemoved()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),  // C4 fundamental
            CreateNote(72, 0.01, 0.99, 0.5f) // C5 octave harmonic (62.5% velocity, quieter)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Single(result);
        Assert.Equal(60, result[0].Pitch.MidiNumber); // Only fundamental remains
    }

    [Fact]
    public void SuppressHarmonics_OctaveHarmonic_LouderHarmonicKept()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),   // C4 fundamental
            CreateNote(72, 0.01, 0.99, 0.85f) // C5 octave (106% velocity, louder - likely played)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Equal(2, result.Count); // Both notes kept
        Assert.Equal(60, result[0].Pitch.MidiNumber);
        Assert.Equal(72, result[1].Pitch.MidiNumber);
    }

    [Fact]
    public void SuppressHarmonics_HarmonicOutlastsFundamental_BothKept()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),   // C4 fundamental (ends at 1.0s)
            CreateNote(72, 0.01, 2.0, 0.5f)   // C5 octave (ends at 2.01s - too long for harmonic)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Equal(2, result.Count); // Both kept (harmonic outlasts fundamental)
    }

    [Fact]
    public void SuppressHarmonics_HarmonicWithinDurationTolerance_Removed()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),    // C4 fundamental (ends at 1.0s)
            CreateNote(72, 0.01, 1.05, 0.5f)   // C5 octave (ends at 1.06s - within 100ms tolerance)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Single(result); // Harmonic removed
        Assert.Equal(60, result[0].Pitch.MidiNumber);
    }

    [Fact]
    public void SuppressHarmonics_Perfect12th_QuieterHarmonicRemoved()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),  // C4 fundamental
            CreateNote(79, 0.01, 0.99, 0.5f) // G5 (perfect 12th = 19 semitones)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Single(result);
        Assert.Equal(60, result[0].Pitch.MidiNumber);
    }

    [Fact]
    public void SuppressHarmonics_TwoOctaves_QuieterHarmonicRemoved()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),  // C4 fundamental
            CreateNote(84, 0.01, 0.99, 0.5f) // C6 (2 octaves = 24 semitones)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Single(result);
        Assert.Equal(60, result[0].Pitch.MidiNumber);
    }

    [Fact]
    public void SuppressHarmonics_PerfectFifth_NotSuppressed()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),  // C4
            CreateNote(67, 0.01, 0.99, 0.5f) // G4 (perfect 5th = 7 semitones, too common in music)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Equal(2, result.Count); // Both kept (perfect 5ths not suppressed)
    }

    [Fact]
    public void SuppressHarmonics_OutsideTemporalWindow_BothKept()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { TemporalWindowMs = 50.0, VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),   // C4
            CreateNote(72, 0.1, 1.0, 0.5f)    // C5 octave but 100ms later (outside 50ms window)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Equal(2, result.Count); // Both kept (outside temporal window)
    }

    [Fact]
    public void SuppressHarmonics_LowerPitchIsHarmonic_NotRemoved()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(72, 0.0, 1.0, 0.8f),  // C5
            CreateNote(60, 0.01, 0.99, 0.5f) // C4 (lower pitch, not a harmonic of C5)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Equal(2, result.Count); // Both kept (only higher pitches can be harmonics)
    }

    [Fact]
    public void SuppressHarmonics_MultipleHarmonics_AllQuieterOnesRemoved()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(60, 0.0, 1.0, 0.8f),   // C4 fundamental
            CreateNote(72, 0.01, 0.99, 0.5f), // C5 octave (62.5% vel)
            CreateNote(84, 0.02, 0.98, 0.4f)  // C6 two octaves (50% vel)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Single(result);
        Assert.Equal(60, result[0].Pitch.MidiNumber); // Only fundamental remains
    }

    [Fact]
    public void SuppressHarmonics_RealWorldScaleExample_FiltersHarmonics()
    {
        // Arrange - Simulating d-scale.wav harmonics with more lenient velocity ratio
        var options = new HarmonicSuppressionOptions { VelocityRatio = 1.2f }; // Aggressive: remove even louder harmonics
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(74, 0.000, 0.645, 0.61f),  // D5
            CreateNote(64, 0.516, 0.516, 0.65f),  // E4 fundamental
            CreateNote(76, 0.516, 0.516, 0.74f),  // E5 harmonic (113% velocity - removed with ratio 1.2)
            CreateNote(69, 2.065, 0.419, 0.74f),  // A4 fundamental
            CreateNote(81, 2.065, 0.452, 0.74f),  // A5 harmonic (100% velocity - removed with ratio 1.2)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Equal(3, result.Count); // With ratio 1.2, E5 and A5 harmonics ARE removed
        Assert.Equal(74, result[0].Pitch.MidiNumber); // D5
        Assert.Equal(64, result[1].Pitch.MidiNumber); // E4
        Assert.Equal(69, result[2].Pitch.MidiNumber); // A4
    }

    [Fact]
    public void SuppressHarmonics_UnsortedInput_SortsAndProcessesCorrectly()
    {
        // Arrange
        var options = new HarmonicSuppressionOptions { VelocityRatio = 0.9f };
        var suppressor = new HarmonicSuppressor(options);
        var notes = new List<NoteEvent>
        {
            CreateNote(72, 0.01, 0.99, 0.5f), // C5 octave (added first but later in time)
            CreateNote(60, 0.0, 1.0, 0.8f),   // C4 fundamental (added second but earlier)
        };

        // Act
        var result = suppressor.SuppressHarmonics(notes);

        // Assert
        Assert.Single(result);
        Assert.Equal(60, result[0].Pitch.MidiNumber); // Fundamental kept despite input order
    }

    [Fact]
    public void SuppressHarmonics_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var suppressor = new HarmonicSuppressor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => suppressor.SuppressHarmonics(null!));
    }

    // Helper method to create test note events
    private static NoteEvent CreateNote(int midiNote, double onsetSeconds, double durationSeconds, float velocity)
    {
        return new NoteEvent(
            MidiNote.Create(midiNote),
            TimeSpan.FromSeconds(onsetSeconds),
            TimeSpan.FromSeconds(durationSeconds),
            Velocity.Create(velocity)
        );
    }
}
