namespace StaffSharp.Core.Tests;

public class TempoTests
{
    [Fact]
    public void Tempo_Create_ShouldAcceptValidBpm()
    {
        // Arrange & Act
        var tempo = Tempo.Create(120);

        // Assert
        Assert.Equal(120, tempo.Bpm);
    }

    [Fact]
    public void Tempo_Create_ShouldThrowForNonPositiveBpm()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Tempo.Create(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Tempo.Create(-10));
    }

    [Fact]
    public void Tempo_BeatDuration_ShouldCalculateCorrectly()
    {
        // Arrange
        var tempo = Tempo.Create(120); // 120 BPM = 2 beats per second = 0.5s per beat

        // Act
        var beatDuration = tempo.BeatDuration;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(0.5), beatDuration);
    }

    [Fact]
    public void Tempo_TimeToBeats_ShouldConvertCorrectly()
    {
        // Arrange
        var tempo = Tempo.Create(120); // 120 BPM = 2 beats per second

        // Act
        var beats = tempo.TimeToBeats(TimeSpan.FromSeconds(3));

        // Assert
        Assert.Equal(6.0, beats); // 3 seconds at 120 BPM = 6 beats
    }

    [Fact]
    public void Tempo_BeatsToTime_ShouldConvertCorrectly()
    {
        // Arrange
        var tempo = Tempo.Create(120); // 120 BPM = 2 beats per second

        // Act
        var time = tempo.BeatsToTime(6.0);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(3), time); // 6 beats at 120 BPM = 3 seconds
    }

    [Fact]
    public void Tempo_CommonTempos_ShouldBeAvailable()
    {
        // Assert
        Assert.Equal(50, Tempo.Largo.Bpm);
        Assert.Equal(70, Tempo.Adagio.Bpm);
        Assert.Equal(90, Tempo.Andante.Bpm);
        Assert.Equal(110, Tempo.Moderato.Bpm);
        Assert.Equal(140, Tempo.Allegro.Bpm);
        Assert.Equal(180, Tempo.Presto.Bpm);
    }

    [Fact]
    public void Tempo_Comparison_ShouldWorkCorrectly()
    {
        // Arrange
        var slow = Tempo.Create(60);
        var fast = Tempo.Create(120);

        // Assert
        Assert.True(fast > slow);
        Assert.True(slow < fast);
        Assert.True(fast >= slow);
        Assert.True(slow <= fast);
    }
}
