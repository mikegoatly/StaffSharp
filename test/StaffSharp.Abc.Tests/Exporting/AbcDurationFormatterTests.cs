namespace StaffSharp.Abc.Tests.Exporting;

using StaffSharp;
using StaffSharp.Abc.Exporting;
using StaffSharp.Notation;

public class AbcDurationFormatterTests
{
    [Fact]
    public void Format_EighthNoteWithDefaultEighth_ReturnsEmpty()
    {
        // Eighth note (1/2 beat) with L:1/8 (default = 1/2 beat) → same → ""
        var duration = SymbolicDuration.Eighth;
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("", result);
    }

    [Fact]
    public void Format_QuarterNoteWithDefaultEighth_Returns2()
    {
        // Quarter note (1 beat) with L:1/8 (default = 1/2 beat) → 2x → "2"
        var duration = SymbolicDuration.Quarter;
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("2", result);
    }

    [Fact]
    public void Format_HalfNoteWithDefaultEighth_Returns4()
    {
        // Half note (2 beats) with L:1/8 (default = 1/2 beat) → 4x → "4"
        var duration = SymbolicDuration.Half;
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("4", result);
    }

    [Fact]
    public void Format_SixteenthNoteWithDefaultEighth_ReturnsSlash()
    {
        // Sixteenth note (1/4 beat) with L:1/8 (default = 1/2 beat) → 1/2x → "/"
        var duration = SymbolicDuration.Sixteenth;
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("/", result);
    }

    [Fact]
    public void Format_ThirtySecondNoteWithDefaultEighth_ReturnsSlash4()
    {
        // 32nd note (1/8 beat) with L:1/8 (default = 1/2 beat) → 1/4x → "/4"
        var duration = SymbolicDuration.ThirtySecond;
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("/4", result);
    }

    [Fact]
    public void Format_QuarterNoteWithDefaultQuarter_ReturnsEmpty()
    {
        // Quarter note (1 beat) with L:1/4 (default = 1 beat) → same → ""
        var duration = SymbolicDuration.Quarter;
        var defaultNoteLength = Rational.Create(1, 4);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("", result);
    }

    [Fact]
    public void Format_EighthNoteWithDefaultQuarter_ReturnsSlash()
    {
        // Eighth note (1/2 beat) with L:1/4 (default = 1 beat) → 1/2x → "/"
        var duration = SymbolicDuration.Eighth;
        var defaultNoteLength = Rational.Create(1, 4);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("/", result);
    }

    [Fact]
    public void Format_HalfNoteWithDefaultQuarter_Returns2()
    {
        // Half note (2 beats) with L:1/4 (default = 1 beat) → 2x → "2"
        var duration = SymbolicDuration.Half;
        var defaultNoteLength = Rational.Create(1, 4);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("2", result);
    }

    [Fact]
    public void Format_DottedQuarterWithDefaultEighth_Returns3()
    {
        // Dotted quarter = 3/8 beat
        // With L:1/8 (= 1/2 beat), multiplier = (3/8) / (1/2) = (3/8) * (2/1) = 3/4
        // Wait, that's not right. Let me recalculate...

        // Dotted quarter note:
        // - Base duration: quarter note (1/4) = 1 beat
        // - Dotted: 1 + 1/2 = 1.5 beats = 3/2 beats
        // Default L:1/8: (1*4)/8 = 1/2 beat
        // Multiplier: (3/2) / (1/2) = 3

        var duration = new SymbolicDuration(NoteDurationBase.Quarter, dots: 1);
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("3", result);
    }

    [Fact]
    public void Format_DottedEighthWithDefaultEighth_Returns3Over2()
    {
        // Dotted eighth note:
        // - Base: eighth (1/8) = 1/2 beat
        // - Dotted: 1/2 + 1/4 = 3/4 beat
        // Default L:1/8: 1/2 beat
        // Multiplier: (3/4) / (1/2) = 3/2

        var duration = new SymbolicDuration(NoteDurationBase.Eighth, dots: 1);
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("3/2", result);
    }

    [Fact]
    public void Format_WholeNoteWithDefaultEighth_Returns8()
    {
        // Whole note = 4 beats
        // Default L:1/8 = 1/2 beat
        // Multiplier: 4 / (1/2) = 8

        var duration = SymbolicDuration.Whole;
        var defaultNoteLength = Rational.Create(1, 8);

        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        Assert.Equal("8", result);
    }

    [Fact]
    public void Format_TripletEighth_ReturnsEmpty()
    {
        // Arrange: Eighth note triplet with default 1/8
        // Base duration (without tuplet): 1/8 note = 1/2 beat
        // Tuplet (3,2): 1/2 * 2/3 = 1/3 beat (but we ignore tuplet for ABC)
        // Default: 1/8 note = 1/2 beat
        // Multiplier: (1/2) / (1/2) = 1 (base duration matches default)
        var duration = SymbolicDuration.TripletEighth;
        var defaultNoteLength = Rational.Create(1, 8);

        // Act
        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        // Assert
        Assert.Equal(string.Empty, result); // No modifier needed, (3 handles tuplet
    }

    [Fact]
    public void Format_TripletQuarter_Returns2()
    {
        // Arrange: Quarter note triplet with default 1/8
        // Base duration (without tuplet): 1/4 note = 1 beat
        // Tuplet (3,2): 1 * 2/3 = 2/3 beat (but we ignore tuplet for ABC)
        // Default: 1/8 note = 1/2 beat
        // Multiplier: 1 / (1/2) = 2 (base duration is 2x default)
        var duration = SymbolicDuration.TripletQuarter;
        var defaultNoteLength = Rational.Create(1, 8);

        // Act
        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        // Assert
        Assert.Equal("2", result); // Quarter note is 2x eighth note
    }

    [Fact]
    public void Format_QuintupletEighth_ReturnsEmpty()
    {
        // Arrange: Eighth note quintuplet with default 1/8
        // Base duration: 1/8 note = 1/2 beat
        // Default: 1/8 note = 1/2 beat
        // Multiplier: 1 (base duration matches default)
        var duration = new SymbolicDuration(NoteDurationBase.Eighth, tuplet: new Tuplet(5, 4));
        var defaultNoteLength = Rational.Create(1, 8);

        // Act
        var result = AbcDurationFormatter.Format(duration, defaultNoteLength);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
