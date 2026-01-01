namespace StaffSharp.Core.Tests;

public class MidiNoteTests
{
    [Fact]
    public void Create_ValidValue_CreatesInstance()
    {
        var m = MidiNote.Create(60f);
        Assert.Equal(60f, m.Value);
    }

    [Fact]
    public void Create_InvalidValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiNote.Create(-1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiNote.Create(128f));
    }

    [Fact]
    public void Add_Operators_WorkCorrectly()
    {
        var m1 = MidiNote.Create(60f);
        var m2 = MidiNote.Create(62f);

        // MidiNote + float => MidiNote
        Assert.Equal(62f, (m1 + 2f).Value);
        
        // MidiNote + MidiNote => MidiNote
        Assert.Equal(MidiNote.Create(122f), m1 + m2);
    }

    [Fact]
    public void Subtract_Operators_WorkCorrectly()
    {
        var m1 = MidiNote.Create(60f);
        var m2 = MidiNote.Create(62f);

        // MidiNote - float => MidiNote
        Assert.Equal(MidiNote.Create(58f), m1 - 2f);

        // MidiNote - MidiNote => can result in out-of-range value
        // Testing valid subtraction
        var m3 = MidiNote.Create(65f);
        var m4 = MidiNote.Create(5f);
        Assert.Equal(MidiNote.Create(60f), m3 - m4);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var m1 = MidiNote.Create(60f);
        var m2 = MidiNote.Create(60f);
        var m3 = MidiNote.Create(61f);

        Assert.Equal(m1, m2);
        Assert.NotEqual(m1, m3);
        Assert.True(m1 == m2);
        Assert.True(m1 != m3);
    }

    [Fact]
    public void Comparison_WorksCorrectly()
    {
        var m1 = MidiNote.Create(60f);
        var m2 = MidiNote.Create(62f);
        Assert.True(m1 < m2);
        Assert.True(m2 > m1);
        Assert.True(m1 <= m2);
        Assert.True(m2 >= m1);
        Assert.True(m1 <= MidiNote.Create(60f));
        Assert.True(m1 >= MidiNote.Create(60f));
    }

    [Fact]
    public void MidiNumber_RoundsCorrectly()
    {
        var m1 = MidiNote.Create(60.2f);
        Assert.Equal(60, m1.MidiNumber);

        var m2 = MidiNote.Create(60.7f);
        Assert.Equal(61, m2.MidiNumber);
    }

    [Fact]
    public void PitchClass_IsCorrect()
    {
        Assert.Equal(PitchClass.A, MidiNote.A4.PitchClass);
        Assert.Equal(PitchClass.ASharp, MidiNote.ASharp4.PitchClass);
        Assert.Equal(PitchClass.ASharp, MidiNote.BFlat4.PitchClass);
        Assert.Equal(PitchClass.B, MidiNote.B4.PitchClass);
        Assert.Equal(PitchClass.C, MidiNote.C4.PitchClass);
        Assert.Equal(PitchClass.CSharp, MidiNote.CSharp4.PitchClass);
        Assert.Equal(PitchClass.CSharp, MidiNote.DFlat4.PitchClass);
        Assert.Equal(PitchClass.D, MidiNote.D4.PitchClass);
        Assert.Equal(PitchClass.DSharp, MidiNote.DSharp4.PitchClass);
        Assert.Equal(PitchClass.DSharp, MidiNote.EFlat4.PitchClass);
        Assert.Equal(PitchClass.E, MidiNote.E4.PitchClass);
        Assert.Equal(PitchClass.F, MidiNote.F4.PitchClass);
        Assert.Equal(PitchClass.FSharp, MidiNote.FSharp4.PitchClass);
        Assert.Equal(PitchClass.FSharp, MidiNote.GFlat4.PitchClass);
        Assert.Equal(PitchClass.G, MidiNote.G4.PitchClass);
        Assert.Equal(PitchClass.GSharp, MidiNote.GSharp4.PitchClass);
        Assert.Equal(PitchClass.GSharp, MidiNote.AFlat4.PitchClass);

        // Verify notes in other octaves also return correct pitch class
        Assert.Equal(PitchClass.A, MidiNote.A5.PitchClass);
        Assert.Equal(PitchClass.GSharp, MidiNote.AFlat3.PitchClass);
    }

    [Fact]
    public void Octave_IsCorrect()
    {
        Assert.Equal(4, MidiNote.C4.Octave);
        Assert.Equal(4, MidiNote.A4.Octave);
        Assert.Equal(3, MidiNote.C3.Octave);
        Assert.Equal(5, MidiNote.C5.Octave);
    }

    [Fact]
    public void ToFrequency_ConvertsCorrectly()
    {
        var a4 = MidiNote.A4;
        var frequency = a4.ToFrequency();
        
        Assert.Equal(440f, frequency.Value, precision: 2);

        var c4 = MidiNote.C4;
        var c4Freq = c4.ToFrequency();
        Assert.Equal(261.63f, c4Freq.Value, precision: 1);
    }

    [Fact]
    public void FromPitchClass_CreatesCorrectNote()
    {
        var c4 = MidiNote.FromPitchClass(PitchClass.C, 4);
        Assert.Equal(MidiNote.C4, c4);

        var a4 = MidiNote.FromPitchClass(PitchClass.A, 4);
        Assert.Equal(MidiNote.A4, a4);

        var g5 = MidiNote.FromPitchClass(PitchClass.G, 5);
        Assert.Equal(MidiNote.G5, g5);
    }

    [Fact]
    public void FromPitchClass_InvalidPitchClass_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            MidiNote.FromPitchClass((PitchClass)12, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            MidiNote.FromPitchClass((PitchClass)(-1), 4));
    }

    [Fact]
    public void PredefinedConstants_AreCorrect()
    {
        Assert.Equal(60f, MidiNote.C4.Value);
        Assert.Equal(69f, MidiNote.A4.Value);
        Assert.Equal(72f, MidiNote.C5.Value);
        Assert.Equal(48f, MidiNote.C3.Value);
    }

    [Fact]
    public void FromFrequency_A440_ReturnsMidi69()
    {
        var note = MidiNote.FromFrequency(440.0);
        Assert.Equal(MidiNote.A4, note);
        Assert.Equal(69, note.MidiNumber);
    }

    [Fact]
    public void FromFrequency_C4_ReturnsMidi60()
    {
        // C4 = 261.63 Hz
        var note = MidiNote.FromFrequency(261.63);
        Assert.Equal(MidiNote.C4, note);
        Assert.Equal(60, note.MidiNumber);
    }

    [Fact]
    public void FromFrequency_VariousNotes_ReturnsCorrectMidi()
    {
        // E4 = 329.63 Hz (MIDI 64)
        var e4 = MidiNote.FromFrequency(329.63);
        Assert.Equal(64, e4.MidiNumber);

        // G4 = 392.00 Hz (MIDI 67)
        var g4 = MidiNote.FromFrequency(392.00);
        Assert.Equal(67, g4.MidiNumber);

        // A3 = 220.00 Hz (MIDI 57)
        var a3 = MidiNote.FromFrequency(220.00);
        Assert.Equal(57, a3.MidiNumber);

        // A5 = 880.00 Hz (MIDI 81)
        var a5 = MidiNote.FromFrequency(880.00);
        Assert.Equal(81, a5.MidiNumber);
    }

    [Fact]
    public void FromFrequency_RoundsToNearestSemitone()
    {
        // Slightly sharp A4 (442 Hz) should round to MIDI 69
        var sharpA4 = MidiNote.FromFrequency(442.0);
        Assert.Equal(69, sharpA4.MidiNumber);

        // Slightly flat A4 (438 Hz) should still round to MIDI 69
        var flatA4 = MidiNote.FromFrequency(438.0);
        Assert.Equal(69, flatA4.MidiNumber);

        // Halfway between A4 and A#4 should round to nearest
        // A4 = 440 Hz, A#4 = 466.16 Hz, midpoint = 453.08 Hz
        var midpoint = MidiNote.FromFrequency(453.08);
        Assert.InRange(midpoint.MidiNumber, 69, 70);
    }

    [Fact]
    public void FromFrequency_RoundTrip_MatchesToFrequency()
    {
        // Test round-trip conversion: MIDI -> Frequency -> MIDI
        var originalNotes = new[] { MidiNote.C3, MidiNote.A4, MidiNote.G5, MidiNote.C6 };

        foreach (var original in originalNotes)
        {
            var freq = original.ToFrequency();
            var roundTrip = MidiNote.FromFrequency(freq.Value);

            Assert.Equal(original.MidiNumber, roundTrip.MidiNumber);
        }
    }

    [Fact]
    public void FromFrequency_ZeroOrNegative_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiNote.FromFrequency(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiNote.FromFrequency(-440.0));
    }

    [Fact]
    public void FromFrequency_VeryHighFrequency_ThrowsWhenOutOfRange()
    {
        // Frequency too high for MIDI range (above MIDI 127)
        // MIDI 127 = ~12543 Hz
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiNote.FromFrequency(20000.0));
    }

    [Fact]
    public void FromFrequency_VeryLowFrequency_ThrowsWhenOutOfRange()
    {
        // Frequency too low for MIDI range (below MIDI 0)
        // MIDI 0 = ~8.18 Hz
        Assert.Throws<ArgumentOutOfRangeException>(() => MidiNote.FromFrequency(5.0));
    }
}