namespace StaffSharp.Core.Tests;

public class VelocityTests
{
    [Fact]
    public void Create_ValidValue_CreatesInstance()
    {
        var v = Velocity.Create(0.8f);
        Assert.Equal(0.8f, v.Value);
    }

    [Fact]
    public void Create_InvalidValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Velocity.Create(-0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Velocity.Create(1.1f));
    }

    [Fact]
    public void Add_Operators_WorkCorrectly()
    {
        var v1 = Velocity.Create(0.5f);
        var v2 = Velocity.Create(0.3f);

        Assert.Equal(Velocity.Create(0.8f), v1 + v2);
        
        // Test clamping to max 1.0
        var v3 = Velocity.Create(0.8f);
        var v4 = Velocity.Create(0.5f);
        Assert.Equal(Velocity.Create(1.0f), v3 + v4);
    }

    [Fact]
    public void Subtract_Operators_WorkCorrectly()
    {
        var v1 = Velocity.Create(0.5f);
        var v2 = Velocity.Create(0.3f);
        var result = v1 - v2;

        Assert.Equal(0.2f, result.Value, precision: 5);
        
        // Test clamping to min 0.0
        var v3 = Velocity.Create(0.2f);
        var v4 = Velocity.Create(0.5f);
        Assert.Equal(Velocity.Create(0.0f), v3 - v4);
    }

    [Fact]
    public void Comparison_WorksCorrectly()
    {
        var v1 = Velocity.Create(0.5f);
        var v2 = Velocity.Create(0.3f);

        Assert.True(v1 > v2);
        Assert.True(v2 < v1);
        Assert.True(v1 >= v2);
        Assert.True(v2 <= v1);
        Assert.True(v1 >= Velocity.Create(0.5f));
        Assert.True(v1 <= Velocity.Create(0.5f));
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var v1 = Velocity.Create(0.5f);
        var v2 = Velocity.Create(0.5f);
        var v3 = Velocity.Create(0.3f);

        Assert.Equal(v1, v2);
        Assert.NotEqual(v1, v3);
        Assert.True(v1 == v2);
        Assert.True(v1 != v3);
    }

    [Fact]
    public void MidiVelocity_ConvertsCorrectly()
    {
        var v1 = Velocity.Create(0.5f);
        Assert.Equal(63, v1.MidiVelocity);

        var v2 = Velocity.Create(1.0f);
        Assert.Equal(127, v2.MidiVelocity);

        var v3 = Velocity.Create(0.0f);
        Assert.Equal(0, v3.MidiVelocity);
    }

    [Fact]
    public void FromMidi_ConvertsCorrectly()
    {
        var v1 = Velocity.FromMidi(64);
        Assert.Equal(64 / 127f, v1.Value, precision: 3);

        var v2 = Velocity.FromMidi(127);
        Assert.Equal(1.0f, v2.Value);

        var v3 = Velocity.FromMidi(0);
        Assert.Equal(0.0f, v3.Value);
    }

    [Fact]
    public void FromMidi_InvalidValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Velocity.FromMidi(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Velocity.FromMidi(128));
    }

    [Fact]
    public void PredefinedValues_AreCorrect()
    {
        Assert.Equal(0.2f, Velocity.Pianissimo.Value);
        Assert.Equal(0.4f, Velocity.Piano.Value);
        Assert.Equal(0.5f, Velocity.MezzoPiano.Value);
        Assert.Equal(0.6f, Velocity.MezzoForte.Value);
        Assert.Equal(0.8f, Velocity.Forte.Value);
        Assert.Equal(1.0f, Velocity.Fortissimo.Value);
    }
}