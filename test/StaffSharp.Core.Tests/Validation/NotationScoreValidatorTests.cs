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
        AssertValidResult(result);
    }

    [Theory]
    [InlineData(10, "Tempo is too slow: 10 BPM (minimum 20 BPM)")]
    [InlineData(1, "Tempo is too slow: 1 BPM (minimum 20 BPM)")]
    [InlineData(350, "Tempo is too fast: 350 BPM (maximum 300 BPM)")]
    [InlineData(500, "Tempo is too fast: 500 BPM (maximum 300 BPM)")]
    public void Validate_InvalidTempo_ReturnsError(int tempo, string expectedError)
    {
        // Arrange
        var events = NotationEventBuilder.Create()
            .C(duration: SymbolicDuration.Whole)
            .Build();

        var score = CreateScore(TimeSignature.CommonTime, tempo, Clef.Treble, events);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertInvalidResult(result, expectedError);
    }

    [Theory]
    [InlineData(30, "Unusually slow tempo: 30 BPM")]
    [InlineData(250, "Unusually fast tempo: 250 BPM")]
    public void Validate_UnusualTempo_ReturnsWarning(int tempo, string expectedWarning)
    {
        // Arrange
        var events = NotationEventBuilder.Create()
            .C(duration: SymbolicDuration.Whole)
            .Build();

        var score = CreateScore(TimeSignature.CommonTime, tempo, Clef.Treble, events);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertValidResult(result, expectedWarning);
    }

    [Fact]
    public void Validate_IncompleteFinalBar_DoesntFail()
    {
        // Arrange - 3/4 time signature with only 2 beats in final measure
        var events = NotationEventBuilder.Create()
            .C().D() // Only 2 quarter notes = 2 beats
            .Build();

        var score = CreateScore(new TimeSignature(3, 4), 120, Clef.Treble, events);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert (Expect a warning, not an error)
        AssertValidResult(result, "Incomplete first");
    }

    [Fact]
    public void Validate_MeasureDurationMismatch_ReturnsError()
    {
        // Arrange - 4/4 time signature but middle bar only has 2 beats
        var firstBarEvents = NotationEventBuilder.Create()
            .C().D().E().F()
            .Build(); // 4 quarter notes = 4 beats
        var secondBarEvents = NotationEventBuilder.Create()
            .C().D()
            .Build(); // Only 2 quarter notes = 2 beats, but 4/4 requires 4 beats
        var thirdBarEvents = NotationEventBuilder.Create()
            .C().D().E().F()
            .Build(); // 4 quarter notes = 4 beats

        var score = CreateScore(firstBarEvents, secondBarEvents, thirdBarEvents);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertInvalidResult(result, "Duration mismatch", "Expected 4, got 2");
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
        AssertInvalidResult(result, "Score has no parts");
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
        AssertValidResult(result, "Voice 1 has no measures");
    }

    [Fact]
    public void Validate_NoteOutsideMidiRange_ReturnsWarning()
    {
        // Arrange - Note at octave -2 (way below MIDI range)
        var events = new List<INotationEvent>
        {
            new NotationNote(new Pitch(PitchClass.C, -2), SymbolicDuration.Whole, Velocity.MezzoForte)
        };

        var score = CreateScore(TimeSignature.CommonTime, 120, Clef.Bass, events);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert - valid result with warning
        AssertValidResult(result, "outside MIDI range");
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
        AssertInvalidResult(result, "Invalid velocity 1.5");
    }

    // Note: Empty chord validation removed - constructor already enforces minimum 2 pitches

    [Fact]
    public void Validate_ThreeQuarterTimeMeasure_ValidatesCorrectly()
    {
        // Arrange - 3/4 time signature with 3 quarter notes
        var events = NotationEventBuilder.Create()
            .C().D().E()
            .Build();

        var score = CreateScore(new TimeSignature(3, 4), 120, Clef.Treble, events);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertValidResult(result);
    }

    [Fact]
    public void Validate_MeasureWithRests_CalculatesDurationCorrectly()
    {
        // Arrange - Quarter note, quarter rest, half note = 4 beats (valid for 4/4)
        var events = NotationEventBuilder.Create()
            .C().Rest().D(duration: SymbolicDuration.Half)
            .Build();
        var score = CreateScore(events);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertValidResult(result);
    }

    [Theory]
    [InlineData(0, "Invalid time signature numerator: 0")]
    [InlineData(33, "Invalid time signature numerator: 33")]
    [InlineData(-1, "Invalid time signature numerator: -1")]
    public void Validate_InvalidTimeSignatureNumerator_ReturnsError(int numerator, string expectedError)
    {
        // Arrange
        var score = CreateScore(new TimeSignature(numerator, 4), 120, Clef.Treble);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertInvalidResult(result, expectedError);
    }

    [Theory]
    [InlineData(0, "Invalid time signature denominator: 0")]
    [InlineData(65, "Invalid time signature denominator: 65")]
    public void Validate_InvalidTimeSignatureDenominator_ReturnsError(int denominator, string expectedError)
    {
        // Arrange
        var score = CreateScore(new TimeSignature(4, denominator), 120, Clef.Treble);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertInvalidResult(result, expectedError);
    }

    [Fact]
    public void Validate_PickupBar_DoesntFail()
    {
        // Arrange - Pickup bar with 1 beat, followed by complete measures
        var pickupEvents = NotationEventBuilder.Create()
            .C() // Only 1 quarter note = 1 beat
            .Build();
        var fullBarEvents = NotationEventBuilder.Create()
            .C().D().E().F() // 4 quarter notes = 4 beats
            .Build();
        var endingEvents = NotationEventBuilder.Create()
            .C().D().E() // 3 quarter notes = 3 beats (complements the pickup)
            .Build();

        var score = CreateScore(pickupEvents, fullBarEvents, endingEvents);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertValidResult(result, "Incomplete first measure (pickup bar)");
    }

    [Fact]
    public void Validate_PickupBarWithPartialEnding_ValidatesCorrectly()
    {
        // Arrange - 1 beat pickup + 3 beat ending = 4 beats total (valid for 4/4)
        var pickupEvents = NotationEventBuilder.Create()
            .C() // 1 beat
            .Build();
        var middleBarEvents = NotationEventBuilder.Create()
            .C().D().E().F() // 4 beats
            .Build();
        var endingEvents = NotationEventBuilder.Create()
            .C().D().E() // 3 beats
            .Build();

        var score = CreateScore(pickupEvents, middleBarEvents, endingEvents);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertValidResult(result, "Incomplete first measure (pickup bar)", "Incomplete final measure");
    }

    [Fact]
    public void Validate_PickupBarExceedingTimeSignature_ReturnsError()
    {
        // Arrange - 5 beat pickup exceeds 4/4 time signature
        var pickupEvents = NotationEventBuilder.Create()
            .C().D().E().F().G() // 5 beats (too many)
            .Build();
        var endingEvents = NotationEventBuilder.Create()
            .C().D().E() // 3 beats
            .Build();

        var score = CreateScore(pickupEvents, endingEvents);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertInvalidResult(result, "Duration mismatch");
    }

    [Fact]
    public void Validate_SingleMeasurePickupBar_AllowedWithWarning()
    {
        // Arrange - Single incomplete measure (edge case - both first and last)
        var events = NotationEventBuilder.Create()
            .C().D() // 2 beats in a 4/4 measure
            .Build();

        var score = CreateScore(events);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertValidResult(result, "Incomplete first measure (pickup bar)");
    }

    [Fact]
    public void Validate_MiddleMeasureDurationMismatch_StillReturnsError()
    {
        // Arrange - Ensure middle measures still get validated properly
        var pickupEvents = NotationEventBuilder.Create()
            .C() // 1 beat
            .Build();
        var invalidMiddleEvents = NotationEventBuilder.Create()
            .C().D() // Only 2 beats - invalid for middle measure
            .Build();
        var endingEvents = NotationEventBuilder.Create()
            .C().D().E() // 3 beats
            .Build();

        var score = CreateScore(pickupEvents, invalidMiddleEvents, endingEvents);

        // Act
        var result = NotationScoreValidator.Validate(score);

        // Assert
        AssertInvalidResult(result, "Measure 2", "Duration mismatch");
    }

    // Helper methods
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

    /// <summary>
    /// Creates a score with the specified measures. Uses common defaults for metadata.
    /// </summary>
    private static NotationScore CreateScore(
        params IReadOnlyList<INotationEvent>[] measuresEvents)
    {
        return CreateScore(TimeSignature.CommonTime, 120, Clef.Treble, measuresEvents);
    }

    /// <summary>
    /// Creates a score with full customization options.
    /// </summary>
    private static NotationScore CreateScore(
        TimeSignature timeSignature,
        int tempo,
        Clef clef, 
        params IReadOnlyList<INotationEvent>[] measuresEvents)
    {
        var metadata = new ScoreMetadata("Test", "Composer", KeySignature.C, timeSignature, tempo);
        var measures = measuresEvents.Select((events, index) => new Measure(index + 1, events)).ToList();

        return new NotationScore(metadata, [
            new Part("Part", clef, [
                new Voice(1, measures)
            ])
        ]);
    }

    /// <summary>
    /// Asserts that validation result is valid with no errors or warnings.
    /// </summary>
    private static void AssertValidResult(ValidationResult result)
    {
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// Asserts that validation result is valid with expected warnings.
    /// </summary>
    private static void AssertValidResult(ValidationResult result, params string[] expectedWarnings)
    {
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        foreach (var warning in expectedWarnings)
        {
            Assert.Contains(result.Warnings, w => w.Contains(warning));
        }
    }

    /// <summary>
    /// Asserts that validation result is invalid with expected errors.
    /// </summary>
    private static void AssertInvalidResult(ValidationResult result, params string[] expectedErrors)
    {
        Assert.False(result.IsValid);
        foreach (var error in expectedErrors)
        {
            Assert.Contains(result.Errors, e => e.Contains(error));
        }
    }
}
