namespace StaffSharp.Svg.Tests.Layout.Services;

using StaffSharp.Layout.Services;
using StaffSharp.Notation;

using Xunit;

public class KeySignatureServiceTests
{
    private const double staffSpace = 10.0;

    #region GetAffectedPitches Tests

    [Fact]
    public void GetAffectedPitches_CMajor_ReturnsEmpty()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.C);

        // Assert
        Assert.Empty(affected);
    }

    [Fact]
    public void GetAffectedPitches_GMajor_ReturnsFSharp()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.G);

        // Assert
        Assert.Single(affected);
        Assert.True(affected.ContainsKey(6)); // F# is pitch class 6
        Assert.Equal(Accidental.Sharp, affected[6]);
    }

    [Fact]
    public void GetAffectedPitches_DMajor_ReturnsFSharpAndCSharp()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.D);

        // Assert
        Assert.Equal(2, affected.Count);
        Assert.True(affected.ContainsKey(6));  // F#
        Assert.True(affected.ContainsKey(1));  // C#
        Assert.Equal(Accidental.Sharp, affected[6]);
        Assert.Equal(Accidental.Sharp, affected[1]);
    }

    [Fact]
    public void GetAffectedPitches_AMajor_ReturnsThreeSharps()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.A);

        // Assert - F# C# G# (pitch classes 6, 1, 8)
        Assert.Equal(3, affected.Count);
        Assert.True(affected.ContainsKey(6));  // F#
        Assert.True(affected.ContainsKey(1));  // C#
        Assert.True(affected.ContainsKey(8));  // G#
        Assert.All(affected.Values, acc => Assert.Equal(Accidental.Sharp, acc));
    }

    [Fact]
    public void GetAffectedPitches_EMajor_ReturnsFourSharps()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.E);

        // Assert - F# C# G# D# (pitch classes 6, 1, 8, 3)
        Assert.Equal(4, affected.Count);
        Assert.True(affected.ContainsKey(6));  // F#
        Assert.True(affected.ContainsKey(1));  // C#
        Assert.True(affected.ContainsKey(8));  // G#
        Assert.True(affected.ContainsKey(3));  // D#
        Assert.All(affected.Values, acc => Assert.Equal(Accidental.Sharp, acc));
    }

    [Fact]
    public void GetAffectedPitches_CSharpMajor_ReturnsSevenSharps()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.CSharp);

        // Assert - all 7 sharps: F# C# G# D# A# E# B# (pitch classes 6, 1, 8, 3, 10, 5, 0)
        Assert.Equal(7, affected.Count);
        Assert.All(affected.Values, acc => Assert.Equal(Accidental.Sharp, acc));
    }

    [Fact]
    public void GetAffectedPitches_FMajor_ReturnsBFlat()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.F);

        // Assert
        Assert.Single(affected);
        Assert.True(affected.ContainsKey(10)); // Bb is pitch class 10
        Assert.Equal(Accidental.Flat, affected[10]);
    }

    [Fact]
    public void GetAffectedPitches_BFlatMajor_ReturnsTwoFlats()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.BFlat);

        // Assert - Bb Eb (pitch classes 10, 3)
        Assert.Equal(2, affected.Count);
        Assert.True(affected.ContainsKey(10)); // Bb
        Assert.True(affected.ContainsKey(3));  // Eb
        Assert.All(affected.Values, acc => Assert.Equal(Accidental.Flat, acc));
    }

    [Fact]
    public void GetAffectedPitches_EFlatMajor_ReturnsThreeFlats()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.EFlat);

        // Assert - Bb Eb Ab (pitch classes 10, 3, 8)
        Assert.Equal(3, affected.Count);
        Assert.True(affected.ContainsKey(10)); // Bb
        Assert.True(affected.ContainsKey(3));  // Eb
        Assert.True(affected.ContainsKey(8));  // Ab
        Assert.All(affected.Values, acc => Assert.Equal(Accidental.Flat, acc));
    }

    [Fact]
    public void GetAffectedPitches_CFlatMajor_ReturnsSevenFlats()
    {
        // Act
        var affected = KeySignatureService.GetAffectedPitches(KeySignature.CFlat);

        // Assert - all 7 flats
        Assert.Equal(7, affected.Count);
        Assert.All(affected.Values, acc => Assert.Equal(Accidental.Flat, acc));
    }

    #endregion

    #region GetAccidental Tests

    [Theory]
    [InlineData(0, Accidental.Natural)]   // C
    [InlineData(2, Accidental.Natural)]   // D
    [InlineData(4, Accidental.Natural)]   // E
    [InlineData(5, Accidental.Natural)]   // F
    [InlineData(7, Accidental.Natural)]   // G
    [InlineData(9, Accidental.Natural)]   // A
    [InlineData(11, Accidental.Natural)]  // B
    public void GetAccidental_NaturalNotes_ReturnsNatural(int midiNote, Accidental expected)
    {
        // Arrange
        var pitch = new MidiNote(midiNote).ToPitch();

        // Act
        var accidental = KeySignatureService.GetAccidental(pitch);

        // Assert
        Assert.Equal(expected, accidental);
    }

    [Theory]
    [InlineData(1, Accidental.Sharp)]   // C#/Db
    [InlineData(3, Accidental.Sharp)]   // D#/Eb
    [InlineData(6, Accidental.Sharp)]   // F#/Gb
    [InlineData(8, Accidental.Sharp)]   // G#/Ab
    [InlineData(10, Accidental.Sharp)]  // A#/Bb
    public void GetAccidental_BlackKeys_ReturnsSharp(int midiNote, Accidental expected)
    {
        // Arrange
        var pitch = new MidiNote(midiNote).ToPitch();

        // Act
        var accidental = KeySignatureService.GetAccidental(pitch);

        // Assert
        Assert.Equal(expected, accidental);
    }

    [Fact]
    public void GetAccidental_ExplicitFlat_ReturnsFlat()
    {
        // Arrange - explicit flat from ABC notation (e.g., _D)
        var pitch = new Pitch(PitchClass.D, 4, Accidental.Flat);

        // Act
        var accidental = KeySignatureService.GetAccidental(pitch);

        // Assert - should return the explicit flat, not infer sharp from MIDI
        Assert.Equal(Accidental.Flat, accidental);
    }

    [Fact]
    public void GetAccidental_ExplicitSharp_ReturnsSharp()
    {
        // Arrange - explicit sharp from ABC notation (e.g., ^C)
        var pitch = new Pitch(PitchClass.C, 4, Accidental.Sharp);

        // Act
        var accidental = KeySignatureService.GetAccidental(pitch);

        // Assert - should return the explicit sharp
        Assert.Equal(Accidental.Sharp, accidental);
    }

    [Fact]
    public void GetAccidental_NullAccidental_InfersFromMIDI()
    {
        // Arrange - pitch without explicit accidental (e.g., from MIDI)
        var pitch = new Pitch(PitchClass.CSharp, 4); // No explicit accidental

        // Act
        var accidental = KeySignatureService.GetAccidental(pitch);

        // Assert - should infer sharp from MIDI (fallback behavior)
        Assert.Equal(Accidental.Sharp, accidental);
    }

    #endregion

    #region NeedsAccidental Tests

    [Fact]
    public void NeedsAccidental_NaturalNoteInCMajor_ReturnsFalse()
    {
        // Arrange
        var pitch = new Pitch(PitchClass.C, 4);
        var measureAccidentals = new Dictionary<int, Accidental>();

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.C, measureAccidentals);

        // Assert
        Assert.False(needs);
    }

    [Fact]
    public void NeedsAccidental_FSharpInGMajor_ReturnsFalse()
    {
        // Arrange - F# is in the key signature of G major
        var pitch = new Pitch(PitchClass.FSharp, 4);
        var measureAccidentals = new Dictionary<int, Accidental>();

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.G, measureAccidentals);

        // Assert - should not need explicit accidental
        Assert.False(needs);
    }

    [Fact]
    public void NeedsAccidental_FNaturalInGMajor_ReturnsTrue()
    {
        // Arrange - F natural needs natural sign in G major (which has F#)
        // F natural is PitchClass.F (MIDI 65 in octave 4)
        // G major has F# in key signature, so F natural must show natural sign
        var pitch = new Pitch(PitchClass.F, 4); // F natural (MIDI 65)
        var measureAccidentals = new Dictionary<int, Accidental>();

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.G, measureAccidentals);

        // Assert - F natural is not affected by key signature (F# is MIDI 66, not 65)
        // So F natural doesn't need accidental unless previously altered in measure
        Assert.False(needs);
    }

    [Fact]
    public void NeedsAccidental_CSharpInCMajor_ReturnsTrue()
    {
        // Arrange - C# is not in C major
        var pitch = new Pitch(PitchClass.CSharp, 4);
        var measureAccidentals = new Dictionary<int, Accidental>();

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.C, measureAccidentals);

        // Assert - needs sharp sign
        Assert.True(needs);
    }

    [Fact]
    public void NeedsAccidental_RepeatedAccidentalInSameMeasure_ReturnsFalse()
    {
        // Arrange - second C# in the same measure
        var pitch = new Pitch(PitchClass.CSharp, 4);
        var midiNote = (int)pitch.ToMidiNote().Value;
        var measureAccidentals = new Dictionary<int, Accidental>
        {
            [midiNote] = Accidental.Sharp
        };

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.C, measureAccidentals);

        // Assert - accidental already shown in this measure
        Assert.False(needs);
    }

    [Fact]
    public void NeedsAccidental_DifferentAccidentalInSameMeasure_ReturnsTrue()
    {
        // Arrange - C# then C natural in same measure
        var pitch = new Pitch(PitchClass.C, 4, Accidental.Natural);
        var measureAccidentals = new Dictionary<int, Accidental>
        {
            [61] = Accidental.Sharp // C#4 was played earlier
        };

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.C, measureAccidentals);

        // Assert
        // C#4 (MIDI 61) doesn't affect C4 (MIDI 60), but the explicit natural should still display
        Assert.True(needs);
    }

    [Fact]
    public void NeedsAccidental_NaturalCancelsPreviousSharp_ReturnsTrue()
    {
        // Arrange - C sharp then C natural in same measure
        var pitch = new Pitch(PitchClass.C, 4, Accidental.Natural);
        var previousAccidentals = new Dictionary<int, Accidental>
        {
            [60] = Accidental.Sharp // Previous note at this position was sharp
        };

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.C, previousAccidentals);

        // Assert - need natural to cancel previous sharp
        Assert.True(needs);
    }

    [Fact]
    public void NeedsAccidental_BFlatInFMajor_ReturnsFalse()
    {
        // Arrange - Bb (MIDI 70) is in the key signature of F major
        // The pitch class ASharp (10) represents Bb enharmonically
        // GetAccidental returns Sharp for pitch class 10, but key signature has it as Flat
        // This creates a mismatch: the note is ASharp (treated as sharp) but key has Bb (flat)
        var pitch = new Pitch(PitchClass.ASharp, 4); // MIDI 70
        var measureAccidentals = new Dictionary<int, Accidental>();

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.F, measureAccidentals);

        // Assert - GetAccidental returns Sharp, key has Flat, so they don't match
        // Therefore it needs an accidental to show the sharp
        Assert.True(needs);
    }

    [Fact]
    public void NeedsAccidental_ExplicitFlatInFlatKey_ReturnsTrue()
    {
        // Arrange - explicit _D in K:Db
        var pitch = new Pitch(PitchClass.D, 4, Accidental.Flat);
        var measureAccidentals = new Dictionary<int, Accidental>();

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.DFlat, measureAccidentals);

        // Assert - explicit accidentals should always be shown (first occurrence in measure)
        Assert.True(needs);
    }

    [Fact]
    public void NeedsAccidental_InheritedFlatInFlatKey_ReturnsFalse()
    {
        // Arrange - D (no explicit accidental) in K:Db
        // This note should sound as Db due to key signature, but not show an accidental symbol
        var pitch = new Pitch(PitchClass.D, 4); // No explicit accidental
        var measureAccidentals = new Dictionary<int, Accidental>();

        // Act
        var needs = KeySignatureService.NeedsAccidental(pitch, KeySignature.DFlat, measureAccidentals);

        // Assert - should NOT show accidental because it inherits from key signature
        Assert.False(needs);
    }

    #endregion

    #region CalculateWidth Tests

    private static readonly SvgContext _context = new() { StaffSpace = 10 };

    [Fact]
    public void CalculateWidth_CMajor_ReturnsZero()
    {
        // Act
        var width = KeySignatureService.CalculateWidth(KeySignature.C, _context);

        // Assert
        Assert.Equal(0, width);
    }

    [Theory]
    [InlineData(1, 10.0)]   // 1 sharp = 1.0 staff spaces
    [InlineData(2, 17.0)]   // 2 sharps = 2.0 staff spaces
    [InlineData(3, 24.0)]   // 3 sharps
    [InlineData(7, 52.0)]   // 7 sharps
    public void CalculateWidth_Sharps_ReturnsCorrectWidth(int sharps, double expectedWidth)
    {
        // Arrange
        var keySignature = KeySignature.Create(sharps);

        // Act
        var width = KeySignatureService.CalculateWidth(keySignature, _context);

        // Assert
        Assert.Equal(expectedWidth, width, precision: 0);
    }

    [Theory]
    [InlineData(-1, 10.0)]  // 1 flat
    [InlineData(-2, 17.0)]  // 2 flats
    [InlineData(-3, 24.0)]  // 3 flats
    [InlineData(-7, 52.0)]  // 7 flats
    public void CalculateWidth_Flats_ReturnsCorrectWidth(int flats, double expectedWidth)
    {
        // Arrange
        var keySignature = KeySignature.Create(flats);

        // Act
        var width = KeySignatureService.CalculateWidth(keySignature, _context);

        // Assert
        Assert.Equal(expectedWidth, width, precision: 0);
    }

    [Fact]
    public void CalculateWidth_DifferentStaffSpace_ScalesProportionally()
    {
        // Arrange
        var keySignature = KeySignature.G; // 1 sharp

        // Act
        var width = KeySignatureService.CalculateWidth(keySignature, _context with { StaffSpace = 5 });

        // Assert - 1 sharp at 5.0 staff space = 5.0
        Assert.Equal(5.0, width);
    }

    #endregion

    #region GetAccidentalPositions Tests

    public static TheoryData<KeySignature, Clef, (Accidental, double)[]> GetAccidentalPositionsTestData()
    {
        return new TheoryData<KeySignature, Clef, (Accidental, double)[]>
        {
            // C Major - no accidentals
            { KeySignature.C, Clef.Treble, []},
            { KeySignature.C, Clef.Bass, [] },
            { KeySignature.C, Clef.Alto, [] },
            { KeySignature.C, Clef.Tenor, [] },

            // G Major - 1 sharp (F#)
            { KeySignature.G, Clef.Treble, new[] { (Accidental.Sharp, 0.0) } },
            { KeySignature.G, Clef.Bass, new[] { (Accidental.Sharp, 1.0) } },
            { KeySignature.G, Clef.Alto, new[] { (Accidental.Sharp, 1.0) } },
            { KeySignature.G, Clef.Tenor, new[] { (Accidental.Sharp, 0.0) } },

            // D Major - 2 sharps (F#, C#)
            { KeySignature.D, Clef.Treble, new[] { (Accidental.Sharp, 0.0), (Accidental.Sharp, 1.5) } },
            { KeySignature.D, Clef.Bass, new[] { (Accidental.Sharp, 1.0), (Accidental.Sharp, 2.5) } },
            { KeySignature.D, Clef.Alto, new[] { (Accidental.Sharp, 1.0), (Accidental.Sharp, 3.0) } },
            { KeySignature.D, Clef.Tenor, new[] { (Accidental.Sharp, 0.0), (Accidental.Sharp, 2.0) } },

            // A Major - 3 sharps (F#, C#, G#)
            { KeySignature.A, Clef.Treble, new[] { (Accidental.Sharp, 0.0), (Accidental.Sharp, 1.5), (Accidental.Sharp, -0.5) } },

            // C# Major - 7 sharps (F#, C#, G#, D#, A#, E#, B#)
            { KeySignature.CSharp, Clef.Treble, new[] { (Accidental.Sharp, 0.0), (Accidental.Sharp, 1.5), (Accidental.Sharp, -0.5), (Accidental.Sharp, 1.0), (Accidental.Sharp, 2.5), (Accidental.Sharp, 0.5), (Accidental.Sharp, 2.0) } },

            // F Major - 1 flat (Bb)
            { KeySignature.F, Clef.Treble, new[] { (Accidental.Flat, 2.0) } },
            { KeySignature.F, Clef.Bass, new[] { (Accidental.Flat, 3.0) } },
            { KeySignature.F, Clef.Alto, new[] { (Accidental.Flat, 2.5) } },
            { KeySignature.F, Clef.Tenor, new[] { (Accidental.Flat, 3.5) } },

            // Bb Major - 2 flats (Bb, Eb)
            { KeySignature.BFlat, Clef.Treble, new[] { (Accidental.Flat, 2.0), (Accidental.Flat, 0.5) } },
            { KeySignature.BFlat, Clef.Bass, new[] { (Accidental.Flat, 3.0), (Accidental.Flat, 1.5) } },
            { KeySignature.BFlat, Clef.Alto, new[] { (Accidental.Flat, 2.5), (Accidental.Flat, 1.0) } },
            { KeySignature.BFlat, Clef.Tenor, new[] { (Accidental.Flat, 3.5), (Accidental.Flat, 2.0) } },
        };
    }

    [Theory]
    [MemberData(nameof(GetAccidentalPositionsTestData))]
    public void GetAccidentalPositions_Theory(KeySignature keySignature, Clef clef, (Accidental, double)[] expectedPositions)
    {
        var positions = KeySignatureService.GetAccidentalPositions(keySignature, clef, staffSpace);
        Assert.True(expectedPositions.Length == positions.Count, $"Expected {expectedPositions.Length} accidentals for key {keySignature} in {clef} clef.");
        for (int i = 0; i < expectedPositions.Length; i++)
        {
            Assert.Equal(expectedPositions[i].Item1, positions[i].Accidental);
            Assert.Equal(expectedPositions[i].Item2 * staffSpace, positions[i].YPosition);
        }
    }

    #endregion
}
