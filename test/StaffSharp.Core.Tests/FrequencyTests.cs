namespace StaffSharp.Core.Tests;

public class FrequencyTests
{
    [Fact]
    public void Create_ValidValue_CreatesInstance()
    {
        var f = Frequency.Create(440f);
        Assert.Equal(440f, f.Value);
    }

    [Fact]
    public void Create_InvalidValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Frequency.Create(0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Frequency.Create(-1f));
    }

    [Fact]
    public void Add_Operators_WorkCorrectly()
    {
        var f1 = Frequency.Create(440f);
        var f2 = Frequency.Create(220f);

        Assert.Equal(Frequency.Create(660f), f1 + f2);
        Assert.Equal(Frequency.Create(460f), f1 + 20f);
    }

    [Fact]
    public void Subtract_Operators_WorkCorrectly()
    {
        var f1 = Frequency.Create(440f);
        var f2 = Frequency.Create(220f);

        Assert.Equal(Frequency.Create(220f), f1 - f2);
        Assert.Equal(Frequency.Create(420f), f1 - 20f);
    }

    [Fact]
    public void Multiply_Operators_WorkCorrectly()
    {
        var f1 = Frequency.Create(440f);
        var f2 = Frequency.Create(2f);

        Assert.Equal(Frequency.Create(880f), f1 * f2);
        Assert.Equal(Frequency.Create(880f), f1 * 2f);
    }

    [Fact]
    public void Divide_Operators_WorkCorrectly()
    {
        var f1 = Frequency.Create(440f);
        var f2 = Frequency.Create(2f);

        Assert.Equal(Frequency.Create(220f), f1 / f2);
        Assert.Equal(Frequency.Create(220f), f1 / 2f);
    }

    [Fact]
    public void Comparison_WorksCorrectly()
    {
        var f1 = Frequency.Create(440f);
        var f2 = Frequency.Create(220f);

        Assert.True(f1 > f2);
        Assert.True(f2 < f1);
        Assert.True(f1 >= f2);
        Assert.True(f2 <= f1);
        Assert.True(f1 >= Frequency.Create(440f));
        Assert.True(f1 <= Frequency.Create(440f));
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var f1 = Frequency.Create(440f);
        var f2 = Frequency.Create(440f);
        var f3 = Frequency.Create(442f);

        Assert.Equal(f1, f2);
        Assert.NotEqual(f1, f3);
        Assert.True(f1 == f2);
        Assert.True(f1 != f3);
    }

    [Fact]
    public void ToMidiNote_ConvertsCorrectly()
    {
        var a4 = Frequency.A4;
        var midiNote = a4.ToMidiNote();
        
        Assert.Equal(69f, midiNote.Value, precision: 2);
    }

    [Fact]
    public void FromMidiNote_ConvertsCorrectly()
    {
        var midiNote = MidiNote.A4;
        var frequency = Frequency.FromMidiNote(midiNote);
        
        Assert.Equal(440f, frequency.Value, precision: 2);
    }

    [Fact]
    public void PredefinedValues_AreCorrect()
    {
        Assert.Equal(440f, Frequency.A4.Value);
        Assert.Equal(261.63f, Frequency.C4.Value);
    }
}