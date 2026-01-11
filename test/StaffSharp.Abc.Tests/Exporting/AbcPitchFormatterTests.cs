namespace StaffSharp.Abc.Tests.Exporting;

using StaffSharp.Abc.Exporting;
using StaffSharp.Notation;

public class AbcPitchFormatterTests
{
    [Theory]
    [InlineData(PitchClass.C, 4, null, "C")]
    [InlineData(PitchClass.D, 4, null, "D")]
    [InlineData(PitchClass.E, 4, null, "E")]
    [InlineData(PitchClass.F, 4, null, "F")]
    [InlineData(PitchClass.G, 4, null, "G")]
    [InlineData(PitchClass.A, 4, null, "A")]
    [InlineData(PitchClass.B, 4, null, "B")]
    public void Format_Octave4Notes_ReturnsUppercase(PitchClass pitchClass, int octave, Accidental? accidental, string expected)
    {
        var pitch = new Pitch(pitchClass, octave, accidental);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PitchClass.C, 5, null, "c")]
    [InlineData(PitchClass.D, 5, null, "d")]
    [InlineData(PitchClass.E, 5, null, "e")]
    [InlineData(PitchClass.F, 5, null, "f")]
    [InlineData(PitchClass.G, 5, null, "g")]
    [InlineData(PitchClass.A, 5, null, "a")]
    [InlineData(PitchClass.B, 5, null, "b")]
    public void Format_Octave5Notes_ReturnsLowercase(PitchClass pitchClass, int octave, Accidental? accidental, string expected)
    {
        var pitch = new Pitch(pitchClass, octave, accidental);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PitchClass.C, 4, Accidental.Sharp, "^C")]
    [InlineData(PitchClass.D, 4, Accidental.Flat, "_D")]
    [InlineData(PitchClass.E, 4, Accidental.Natural, "=E")]
    [InlineData(PitchClass.F, 4, Accidental.DoubleSharp, "^^F")]
    [InlineData(PitchClass.G, 4, Accidental.DoubleFlat, "__G")]
    [InlineData(PitchClass.A, 4, Accidental.QuarterSharp, "^/A")]
    [InlineData(PitchClass.B, 4, Accidental.QuarterFlat, "_/B")]
    [InlineData(PitchClass.C, 4, Accidental.ThreeQuarterSharp, "^3/C")]
    [InlineData(PitchClass.D, 4, Accidental.ThreeQuarterFlat, "_3/D")]
    public void Format_WithAccidentals_ReturnsCorrectSymbol(PitchClass pitchClass, int octave, Accidental? accidental, string expected)
    {
        var pitch = new Pitch(pitchClass, octave, accidental);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_Octave3_AddsComma()
    {
        // Octave 3 (one octave below default 4) → adds one comma
        var pitch = new Pitch(PitchClass.C, 3);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal("C,", result);
    }

    [Fact]
    public void Format_Octave2_AddsTwoCommas()
    {
        // Octave 2 (two octaves below default 4) → adds two commas
        var pitch = new Pitch(PitchClass.C, 2);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal("C,,", result);
    }

    [Fact]
    public void Format_Octave6_AddsApostrophe()
    {
        // Octave 6 (one octave above default 5 for lowercase) → adds one apostrophe
        var pitch = new Pitch(PitchClass.C, 6);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal("c'", result);
    }

    [Fact]
    public void Format_Octave7_AddsTwoApostrophes()
    {
        // Octave 7 (two octaves above default 5 for lowercase) → adds two apostrophes
        var pitch = new Pitch(PitchClass.C, 7);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal("c''", result);
    }

    [Fact]
    public void Format_SharpOctave5_ReturnsLowercaseWithSharp()
    {
        // F# in octave 5 should be "^f" (lowercase with sharp)
        var pitch = new Pitch(PitchClass.F, 5, Accidental.Sharp);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal("^f", result);
    }

    [Fact]
    public void Format_FlatOctave3_ReturnsUppercaseWithFlatAndComma()
    {
        // Bb in octave 3 should be "_B," (uppercase B with flat and comma)
        var pitch = new Pitch(PitchClass.B, 3, Accidental.Flat);
        var result = AbcPitchFormatter.Format(pitch);
        Assert.Equal("_B,", result);
    }
}
