namespace StaffSharp.Core.Tests.Notation;

using StaffSharp;
using StaffSharp.Notation;

public class PitchTests
{
    [Fact]
    public void ToMidiNote_C4_Returns60()
    {
        var c4 = new Pitch(PitchClass.C, 4);
        Assert.Equal(MidiNote.C4, c4.ToMidiNote());
    }

    [Fact]
    public void ToMidiNote_A4_Returns69()
    {
        var a4 = new Pitch(PitchClass.A, 4);
        Assert.Equal(MidiNote.A4, a4.ToMidiNote());
    }

    [Fact]
    public void ToMidiNote_WithSharp_AddsOne()
    {
        var cSharp4 = new Pitch(PitchClass.C, 4, Accidental.Sharp);
        Assert.Equal(MidiNote.CSharp4, cSharp4.ToMidiNote());
    }

    [Fact]
    public void ToMidiNote_WithFlat_SubtractsOne()
    {
        var bFlat4 = new Pitch(PitchClass.B, 4, Accidental.Flat);
        Assert.Equal(MidiNote.BFlat4, bFlat4.ToMidiNote());
    }

    [Fact]
    public void ToMidiNote_WithNatural_NoChange()
    {
        var e4 = new Pitch(PitchClass.E, 4, Accidental.Natural);
        Assert.Equal(MidiNote.E4, e4.ToMidiNote());
    }
}
