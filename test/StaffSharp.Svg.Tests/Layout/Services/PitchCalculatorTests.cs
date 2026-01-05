namespace StaffSharp.Svg.Tests.Layout.Services;

using StaffSharp.Layout.Services;
using StaffSharp.Notation;

using Xunit;

public class PitchCalculatorTests
{
    [Theory]
    [InlineData(PitchClass.B, 4, Clef.Treble, 0)]  // B4 on middle line of treble clef
    [InlineData(PitchClass.C, 5, Clef.Treble, 1)]  // C5 one position above middle line
    [InlineData(PitchClass.A, 4, Clef.Treble, -1)] // A4 one position below middle line
    [InlineData(PitchClass.G, 4, Clef.Treble, -2)] // G4 two positions below middle line
    [InlineData(PitchClass.D, 5, Clef.Treble, 2)]  // D5 two positions above middle line
    [InlineData(PitchClass.E, 5, Clef.Treble, 3)]  // E5 three positions above middle line
    [InlineData(PitchClass.F, 4, Clef.Treble, -3)] // F4 three positions below middle line
    public void PitchToStaffPosition_TrebleClef_ReturnsCorrectPosition(
        PitchClass pitchClass,
        int octave,
        Clef clef,
        int expectedPosition)
    {
        // Arrange
        var pitch = new Pitch(pitchClass, octave);

        // Act
        var position = PitchCalculator.PitchToStaffPosition(pitch, clef);

        // Assert
        Assert.Equal(expectedPosition, position);
    }

    [Theory]
    [InlineData(PitchClass.D, 3, Clef.Bass, 0)]   // D3 on middle line of bass clef
    [InlineData(PitchClass.E, 3, Clef.Bass, 1)]   // E3 one position above
    [InlineData(PitchClass.C, 3, Clef.Bass, -1)]  // C3 one position below
    [InlineData(PitchClass.F, 3, Clef.Bass, 2)]   // F3 two positions above
    [InlineData(PitchClass.A, 2, Clef.Bass, -3)]  // A2 three positions below
    [InlineData(PitchClass.G, 3, Clef.Bass, 3)]   // G3 three positions above
    [InlineData(PitchClass.B, 2, Clef.Bass, -2)]  // B2 two positions below
    public void PitchToStaffPosition_BassClef_ReturnsCorrectPosition(
        PitchClass pitchClass,
        int octave,
        Clef clef,
        int expectedPosition)
    {
        // Arrange
        var pitch = new Pitch(pitchClass, octave);

        // Act
        var position = PitchCalculator.PitchToStaffPosition(pitch, clef);

        // Assert
        Assert.Equal(expectedPosition, position);
    }

    [Theory]
    [InlineData(PitchClass.C, 4, Clef.Alto, 0)]   // C4 on middle line of alto clef
    [InlineData(PitchClass.D, 4, Clef.Alto, 1)]   // D4 one position above
    [InlineData(PitchClass.B, 3, Clef.Alto, -1)]  // B3 one position below
    [InlineData(PitchClass.E, 4, Clef.Alto, 2)]   // E4 two positions above
    [InlineData(PitchClass.A, 3, Clef.Alto, -2)]  // A3 two positions below
    public void PitchToStaffPosition_AltoClef_ReturnsCorrectPosition(
        PitchClass pitchClass,
        int octave,
        Clef clef,
        int expectedPosition)
    {
        // Arrange
        var pitch = new Pitch(pitchClass, octave);

        // Act
        var position = PitchCalculator.PitchToStaffPosition(pitch, clef);

        // Assert
        Assert.Equal(expectedPosition, position);
    }

    [Theory]
    [InlineData(PitchClass.A, 3, Clef.Tenor, 0)]  // A3 on middle line of tenor clef
    [InlineData(PitchClass.B, 3, Clef.Tenor, 1)]  // B3 one position above
    [InlineData(PitchClass.G, 3, Clef.Tenor, -1)] // G3 one position below
    [InlineData(PitchClass.C, 4, Clef.Tenor, 2)]  // C4 two positions above
    [InlineData(PitchClass.F, 3, Clef.Tenor, -2)] // F3 two positions below
    public void PitchToStaffPosition_TenorClef_ReturnsCorrectPosition(
        PitchClass pitchClass,
        int octave,
        Clef clef,
        int expectedPosition)
    {
        // Arrange
        var pitch = new Pitch(pitchClass, octave);

        // Act
        var position = PitchCalculator.PitchToStaffPosition(pitch, clef);

        // Assert
        Assert.Equal(expectedPosition, position);
    }

    [Theory]
    [InlineData(PitchClass.CSharp, 5, Clef.Treble)] // C#5
    [InlineData(PitchClass.FSharp, 4, Clef.Treble)] // F#4
    [InlineData(PitchClass.GSharp, 3, Clef.Bass)]   // G#3
    [InlineData(PitchClass.DSharp, 4, Clef.Alto)]   // D#4
    public void PitchToStaffPosition_AccidentalPitches_ReturnsReasonablePosition(
        PitchClass pitchClass,
        int octave,
        Clef clef)
    {
        // Arrange
        var pitch = new Pitch(pitchClass, octave);

        // Act
        var position = PitchCalculator.PitchToStaffPosition(pitch, clef);

        // Assert - verify it returns a reasonable value (within expected staff range)
        Assert.True(position >= -20 && position <= 20);
    }

    [Theory]
    [InlineData(PitchClass.CSharp, 4, Clef.Treble, PitchClass.C)]   // C#4 should position as C
    [InlineData(PitchClass.DSharp, 4, Clef.Treble, PitchClass.D)]   // D#4 should position as D
    [InlineData(PitchClass.FSharp, 4, Clef.Treble, PitchClass.F)]   // F#4 should position as F
    [InlineData(PitchClass.GSharp, 4, Clef.Treble, PitchClass.G)]   // G#4 should position as G
    [InlineData(PitchClass.ASharp, 4, Clef.Treble, PitchClass.A)]   // A#4 should position as A
    public void PitchToStaffPosition_ChromaticPitchClass_PositionsOnBaseNoteLetter(
        PitchClass chromaticPitchClass,
        int octave,
        Clef clef,
        PitchClass expectedBaseNote)
    {
        // Arrange - chromatic pitch class (e.g., CSharp enum value)
        var chromaticPitch = new Pitch(chromaticPitchClass, octave);
        var baseNotePitch = new Pitch(expectedBaseNote, octave);

        // Act
        var chromaticPosition = PitchCalculator.PitchToStaffPosition(chromaticPitch, clef);
        var baseNotePosition = PitchCalculator.PitchToStaffPosition(baseNotePitch, clef);

        // Assert - chromatic pitch should be positioned at same staff position as its base note
        // E.g., C# (PitchClass.CSharp) should be positioned on the C line/space
        Assert.Equal(baseNotePosition, chromaticPosition);
    }

    [Fact]
    public void PitchToStaffPosition_WithAccidentalAnnotation_PreservesPitchClass()
    {
        // Arrange - F#4 with explicit sharp notation should position like F natural
        var pitch = new Pitch(PitchClass.F, 4, Accidental.Sharp);
        var pitchFNatural = new Pitch(PitchClass.F, 4);

        // Act
        var positionFSharp = PitchCalculator.PitchToStaffPosition(pitch, Clef.Treble);
        var positionFNatural = PitchCalculator.PitchToStaffPosition(pitchFNatural, Clef.Treble);

        // Assert - F# should be positioned at the same staff position as F natural
        // The sharp symbol is rendered separately
        Assert.Equal(positionFNatural, positionFSharp);
    }

    [Theory]
    [InlineData(PitchClass.C, 0, Clef.Treble)] // Very low note
    [InlineData(PitchClass.C, 8, Clef.Treble)] // Very high note
    [InlineData(PitchClass.C, 0, Clef.Bass)]
    [InlineData(PitchClass.C, 8, Clef.Bass)]
    public void PitchToStaffPosition_ExtremeOctaves_ReturnsValidPosition(
        PitchClass pitchClass,
        int octave,
        Clef clef)
    {
        // Arrange
        var pitch = new Pitch(pitchClass, octave);

        // Act
        var position = PitchCalculator.PitchToStaffPosition(pitch, clef);

        // Assert - just verify it doesn't throw and returns something
        Assert.True(position != 0 || octave == 4); // Position 0 only likely for middle octaves
    }

    [Theory]
    [InlineData(Clef.Treble, 71)]  // B4
    [InlineData(Clef.Bass, 50)]    // D3
    [InlineData(Clef.Alto, 60)]    // C4
    [InlineData(Clef.Tenor, 57)]   // A3
    public void GetMiddleLineMidiNote_ReturnsCorrectMidiNote(Clef clef, int expectedMidiNote)
    {
        // Act
        var midiNote = PitchCalculator.GetMiddleLineMidiNote(clef);

        // Assert
        Assert.Equal(expectedMidiNote, midiNote);
    }

    [Fact]
    public void GetMiddleLineMidiNote_UnknownClef_ReturnsDefaultTreble()
    {
        // Act - cast int to invalid Clef value
        var midiNote = PitchCalculator.GetMiddleLineMidiNote((Clef)999);

        // Assert - should default to treble (71)
        Assert.Equal(71, midiNote);
    }

    [Fact]
    public void PitchToStaffPosition_SamePitchDifferentClefs_ReturnsDifferentPositions()
    {
        // Arrange - same pitch C5
        var pitch = new Pitch(PitchClass.C, 5);

        // Act
        var treblePosition = PitchCalculator.PitchToStaffPosition(pitch, Clef.Treble);
        var bassPosition = PitchCalculator.PitchToStaffPosition(pitch, Clef.Bass);
        var altoPosition = PitchCalculator.PitchToStaffPosition(pitch, Clef.Alto);

        // Assert - all should be different
        Assert.NotEqual(treblePosition, bassPosition);
        Assert.NotEqual(treblePosition, altoPosition);
        Assert.NotEqual(bassPosition, altoPosition);
        // Tenor might equal one of them, but not all
        Assert.True(
            treblePosition != bassPosition ||
            treblePosition != altoPosition ||
            bassPosition != altoPosition);
    }

    [Fact]
    public void PitchToStaffPosition_AdjacentPitches_HaveAdjacentPositions()
    {
        // Arrange - C4 and D4 are adjacent diatonic pitches
        var pitchC = new Pitch(PitchClass.C, 4);
        var pitchD = new Pitch(PitchClass.D, 4);

        // Act
        var positionC = PitchCalculator.PitchToStaffPosition(pitchC, Clef.Treble);
        var positionD = PitchCalculator.PitchToStaffPosition(pitchD, Clef.Treble);

        // Assert - D should be one position above C
        Assert.Equal(1, positionD - positionC);
    }

    [Fact]
    public void PitchToStaffPosition_OctaveJump_ChangesPositionBySevenSteps()
    {
        // Arrange - C4 and C5 are one octave apart
        var pitchC4 = new Pitch(PitchClass.C, 4);
        var pitchC5 = new Pitch(PitchClass.C, 5);

        // Act
        var positionC4 = PitchCalculator.PitchToStaffPosition(pitchC4, Clef.Treble);
        var positionC5 = PitchCalculator.PitchToStaffPosition(pitchC5, Clef.Treble);

        // Assert - one octave = 7 diatonic positions
        Assert.Equal(7, positionC5 - positionC4);
    }

    [Theory]
    [InlineData(PitchClass.C, Accidental.Sharp, Clef.Treble)]   // C# positioned as C
    [InlineData(PitchClass.D, Accidental.Flat, Clef.Treble)]    // Db positioned as D
    [InlineData(PitchClass.D, Accidental.Sharp, Clef.Treble)]   // D# positioned as D
    [InlineData(PitchClass.E, Accidental.Flat, Clef.Treble)]    // Eb positioned as E
    [InlineData(PitchClass.F, Accidental.Sharp, Clef.Treble)]   // F# positioned as F
    [InlineData(PitchClass.G, Accidental.Flat, Clef.Treble)]    // Gb positioned as G
    [InlineData(PitchClass.G, Accidental.Sharp, Clef.Treble)]   // G# positioned as G
    [InlineData(PitchClass.A, Accidental.Flat, Clef.Treble)]    // Ab positioned as A
    [InlineData(PitchClass.A, Accidental.Sharp, Clef.Treble)]   // A# positioned as A
    [InlineData(PitchClass.B, Accidental.Flat, Clef.Treble)]    // Bb positioned as B
    [InlineData(PitchClass.C, Accidental.Sharp, Clef.Bass)]     // Test bass clef too
    [InlineData(PitchClass.D, Accidental.Flat, Clef.Bass)]
    [InlineData(PitchClass.G, Accidental.Flat, Clef.Bass)]
    public void PitchToStaffPosition_WithAccidentals_PreservesOriginalPitchClass(
        PitchClass pitchClass,
        Accidental accidental,
        Clef clef)
    {
        // Arrange - create pitch with accidental and without
        var pitchWithAccidental = new Pitch(pitchClass, 4, accidental);
        var pitchNatural = new Pitch(pitchClass, 4);

        // Act
        var positionWithAccidental = PitchCalculator.PitchToStaffPosition(pitchWithAccidental, clef);
        var positionNatural = PitchCalculator.PitchToStaffPosition(pitchNatural, clef);

        // Assert - both should be at the same staff position
        // The accidental symbol is rendered separately, but the letter name is preserved
        Assert.Equal(positionNatural, positionWithAccidental);
    }

    [Theory]
    [InlineData(PitchClass.D, Accidental.Flat, 4, Clef.Treble, -5)]  // Db4
    [InlineData(PitchClass.E, Accidental.Flat, 4, Clef.Treble, -4)]  // Eb4
    [InlineData(PitchClass.G, Accidental.Flat, 4, Clef.Treble, -2)]  // Gb4
    [InlineData(PitchClass.A, Accidental.Flat, 4, Clef.Treble, -1)]  // Ab4
    [InlineData(PitchClass.B, Accidental.Flat, 4, Clef.Treble, 0)]   // Bb4
    public void PitchToStaffPosition_DbMajorScaleNotes_PositionedCorrectly(
        PitchClass pitchClass,
        Accidental accidental,
        int octave,
        Clef clef,
        int expectedPosition)
    {
        // Arrange - notes from Db major scale with explicit flats
        var pitch = new Pitch(pitchClass, octave, accidental);

        // Act
        var position = PitchCalculator.PitchToStaffPosition(pitch, clef);

        // Assert - verify the position matches expected based on letter name, not MIDI
        // Before the fix, Db would be positioned as C (one position lower)
        Assert.Equal(expectedPosition, position);
    }
}
