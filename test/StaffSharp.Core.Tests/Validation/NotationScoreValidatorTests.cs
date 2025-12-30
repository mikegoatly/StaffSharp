namespace StaffSharp.Core.Tests.Validation;

using StaffSharp;
using StaffSharp.Notation;
using StaffSharp.TestHelpers.Builders;
using StaffSharp.Validation;

public class NotationScoreValidatorTests
{
    [Fact]
    public void Validate_ValidScore_ReturnsNoErrors()
    {
        // Arrange
        var score = BuildValidScore();

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData(10, "Tempo is too slow: 10 BPM (minimum 20 BPM)")]
    [InlineData(1, "Tempo is too slow: 1 BPM (minimum 20 BPM)")]
    [InlineData(350, "Tempo is too fast: 350 BPM (maximum 300 BPM)")]
    [InlineData(500, "Tempo is too fast: 500 BPM (maximum 300 BPM)")]
    public void Validate_InvalidTempo_ReturnsError(int tempo, string expectedError)
    {
        // Arrange
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, tempo);
        var events = NotationEventBuilder.Create()
            .C(duration: SymbolicDuration.Whole)
            .Build();

        var score = new NotationScore(metadata, [
            new Part("Test", Clef.Treble, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors);
    }

    [Theory]
    [InlineData(30, "Unusually slow tempo: 30 BPM")]
    [InlineData(250, "Unusually fast tempo: 250 BPM")]
    public void Validate_UnusualTempo_ReturnsWarning(int tempo, string expectedWarning)
    {
        // Arrange
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, tempo);
        var events = NotationEventBuilder.Create()
            .C(duration: SymbolicDuration.Whole)
            .Build();

        var score = new NotationScore(metadata, [
            new Part("Test", Clef.Treble, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.True(result.IsValid); // No errors, just warnings
        Assert.Contains(expectedWarning, result.Warnings);
    }

    [Fact]
    public void Validate_MeasureDurationMismatch_ReturnsError()
    {
        // Arrange - 4/4 time signature but only 2 beats of notes
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, 120);
        var events = NotationEventBuilder.Create()
            .C().D()
            .Build(); // Only 2 quarter notes = 2 beats, but 4/4 requires 4 beats

        var score = new NotationScore(metadata, [
            new Part("Piano", Clef.Treble, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duration mismatch") && e.Contains("Expected 4, got 2"));
    }

    [Fact]
    public void Validate_EmptyScore_ReturnsError()
    {
        // Arrange
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, 120);
        var score = new NotationScore(metadata, []); // No parts

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Score has no parts", result.Errors);
    }

    [Fact]
    public void Validate_EmptyVoice_ReturnsWarning()
    {
        // Arrange
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, 120);
        var score = new NotationScore(metadata, [
            new Part("Piano", Clef.Treble, [
                new Voice(1, []) // Empty voice
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.True(result.IsValid); // Warning, not error
        Assert.Contains(result.Warnings, w => w.Contains("Voice 1 has no measures"));
    }

    [Fact]
    public void Validate_NoteOutsideMidiRange_ReturnsWarning()
    {
        // Arrange - Note at octave -2 (way below MIDI range)
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, 120);
        var events = new List<INotationEvent>
        {
            new NotationNote(new Pitch(PitchClass.C, -2), SymbolicDuration.Whole, Velocity.MezzoForte)
        };

        var score = new NotationScore(metadata, [
            new Part("Bass", Clef.Bass, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.True(result.IsValid); // Warning, not error
        Assert.Contains(result.Warnings, w => w.Contains("outside MIDI range"));
    }

    [Fact]
    public void Validate_InvalidVelocity_ReturnsError()
    {
        // Arrange - Velocity > 1.0 (invalid)
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, 120);
        var events = new List<INotationEvent>
        {
            new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Whole, new Velocity(1.5f)) // Invalid!
        };

        var score = new NotationScore(metadata, [
            new Part("Test", Clef.Treble, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid velocity 1.5"));
    }

    // Note: Empty chord validation removed - constructor already enforces minimum 2 pitches

    [Fact]
    public void Validate_ThreeQuarterTimeMeasure_ValidatesCorrectly()
    {
        // Arrange - 3/4 time signature with 3 quarter notes
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, new TimeSignature(3, 4), 120);
        var events = NotationEventBuilder.Create()
            .C().D().E()
            .Build();

        var score = new NotationScore(metadata, [
            new Part("Waltz", Clef.Treble, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MeasureWithRests_CalculatesDurationCorrectly()
    {
        // Arrange - Quarter note, quarter rest, half note = 4 beats (valid for 4/4)
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, 120);

        var events = NotationEventBuilder.Create()
            .C().Rest().D(duration: SymbolicDuration.Half)
            .Build();

        var score = new NotationScore(metadata, [
            new Part("Test", Clef.Treble, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0, "Invalid time signature numerator: 0")]
    [InlineData(33, "Invalid time signature numerator: 33")]
    [InlineData(-1, "Invalid time signature numerator: -1")]
    public void Validate_InvalidTimeSignatureNumerator_ReturnsError(int numerator, string expectedError)
    {
        // Arrange
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, new TimeSignature(numerator, 4), 120);
        var score = new NotationScore(metadata, [
            new Part("Test", Clef.Treble, [
                new Voice(1, [new Measure(1, [])])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors);
    }

    [Theory]
    [InlineData(0, "Invalid time signature denominator: 0")]
    [InlineData(65, "Invalid time signature denominator: 65")]
    public void Validate_InvalidTimeSignatureDenominator_ReturnsError(int denominator, string expectedError)
    {
        // Arrange
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, new TimeSignature(4, denominator), 120);
        var score = new NotationScore(metadata, [
            new Part("Test", Clef.Treble, [
                new Voice(1, [new Measure(1, [])])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange - Multiple issues: invalid tempo, duration mismatch, invalid velocity
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, TimeSignature.CommonTime, 1); // Invalid tempo
        var events = new List<INotationEvent>
        {
            new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Quarter, new Velocity(1.5f)) // Invalid velocity
        }; // Only 1 beat, not 4 - duration mismatch

        var score = new NotationScore(metadata, [
            new Part("Test", Clef.Treble, [
                new Voice(1, [new Measure(1, events)])
            ])
        ]);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3); // At least 3 errors
        Assert.Contains(result.Errors, e => e.Contains("Tempo is too slow"));
        Assert.Contains(result.Errors, e => e.Contains("Duration mismatch"));
        Assert.Contains(result.Errors, e => e.Contains("Invalid velocity 1.5"));
    }

    // Helper method
    private static NotationScore BuildValidScore()
    {
        var metadata = new ScoreMetadata("Test Score", "Test Composer", KeySignature.C, TimeSignature.CommonTime, 120);
        var events = NotationEventBuilder.Create()
            .C().D().E().F()
            .Build();

        return new NotationScore(metadata, [
            new Part("Piano", Clef.Treble, [
                new Voice(1, [
                    new Measure(1, events)
                ])
            ])
        ]);
    }
}
