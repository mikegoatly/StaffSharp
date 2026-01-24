using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Tests.Performance;

public class TempoMapTests
{
    [Fact]
    public void TempoMap_RequiresAtLeastOneTempo()
    {
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };

        var exception = Assert.Throws<ArgumentException>(() =>
            new TempoMap([], timeSigs));

        Assert.Contains("at least one tempo and time signature", exception.Message);
    }

    [Fact]
    public void TempoMap_RequiresAtLeastOneTimeSignature()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };

        var exception = Assert.Throws<ArgumentException>(() =>
            new TempoMap(tempos, []));

        Assert.Contains("at least one tempo and time signature", exception.Message);
    }

    [Fact]
    public void TempoMap_RequiresTempoAtBeatZero()
    {
        var tempos = new[] { new TempoChange(Rational.Create(1, 1), 120) }; // Start at beat 1
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };

        var exception = Assert.Throws<ArgumentException>(() =>
            new TempoMap(tempos, timeSigs));

        Assert.Contains("First tempo must be at beat 0", exception.Message);
    }

    [Fact]
    public void TempoMap_RequiresTimeSignatureAtBeatZero()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Create(1, 1), new TimeSignature(4, 4)) }; // Start at beat 1

        var exception = Assert.Throws<ArgumentException>(() =>
            new TempoMap(tempos, timeSigs));

        Assert.Contains("First time signature must be at beat 0", exception.Message);
    }

    [Fact]
    public void BeatsToSeconds_ConstantTempo120_ConvertsCorrectly()
    {
        // 120 BPM = 2 beats per second
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // 4 beats at 120 BPM = 2 seconds
        var seconds = tempoMap.BeatsToSeconds(Rational.Create(4, 1));

        Assert.Equal(2.0, seconds, precision: 5);
    }

    [Fact]
    public void BeatsToSeconds_ConstantTempo60_ConvertsCorrectly()
    {
        // 60 BPM = 1 beat per second
        var tempos = new[] { new TempoChange(Rational.Zero, 60) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // 10 beats at 60 BPM = 10 seconds
        var seconds = tempoMap.BeatsToSeconds(Rational.Create(10, 1));

        Assert.Equal(10.0, seconds, precision: 5);
    }

    [Fact]
    public void BeatsToSeconds_WithTempoChange_ConvertsCorrectly()
    {
        // 120 BPM for first 4 beats, then 60 BPM
        var tempos = new[]
        {
            new TempoChange(Rational.Zero, 120),
            new TempoChange(Rational.Create(4, 1), 60)
        };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // First 4 beats at 120 BPM = 2 seconds
        // Next 4 beats at 60 BPM = 4 seconds
        // Total: 6 seconds
        var seconds = tempoMap.BeatsToSeconds(Rational.Create(8, 1));

        Assert.Equal(6.0, seconds, precision: 5);
    }

    [Fact]
    public void SecondsToBeats_ConstantTempo120_ConvertsCorrectly()
    {
        // 120 BPM = 2 beats per second
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // 2 seconds at 120 BPM = 4 beats
        var beats = tempoMap.SecondsToBeats(2.0);

        Assert.Equal(4.0, beats.ToDouble(), precision: 5);
    }

    [Fact]
    public void SecondsToBeats_WithTempoChange_ConvertsCorrectly()
    {
        // 120 BPM for first 4 beats, then 60 BPM
        var tempos = new[]
        {
            new TempoChange(Rational.Zero, 120),
            new TempoChange(Rational.Create(4, 1), 60)
        };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // First 4 beats at 120 BPM = 2 seconds
        // 6 seconds total should give us 8 beats (4 + 4)
        var beats = tempoMap.SecondsToBeats(6.0);

        Assert.Equal(8.0, beats.ToDouble(), precision: 5);
    }

    [Fact]
    public void BeatsToSeconds_AndBack_RoundTrips()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        var originalBeats = Rational.Create(10, 1);
        var seconds = tempoMap.BeatsToSeconds(originalBeats);
        var roundTripBeats = tempoMap.SecondsToBeats(seconds);

        Assert.Equal(originalBeats.ToDouble(), roundTripBeats.ToDouble(), precision: 5);
    }

    [Fact]
    public void GetMeasureAt_Beat0_ReturnsFirstMeasure()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        var location = tempoMap.GetMeasureAt(Rational.Zero);

        Assert.Equal(1, location.MeasureNumber);
        Assert.Equal(Rational.Zero, location.BeatInMeasure);
    }

    [Fact]
    public void GetMeasureAt_4_4Time_CalculatesMeasureCorrectly()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // Beat 8 = measure 3, beat 0 (measures 1 and 2 = 8 beats)
        var location = tempoMap.GetMeasureAt(Rational.Create(8, 1));

        Assert.Equal(3, location.MeasureNumber);
        Assert.Equal(Rational.Zero, location.BeatInMeasure);
    }

    [Fact]
    public void GetMeasureAt_MiddleOfMeasure_ReturnsCorrectBeat()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // Beat 10.5 = measure 3, beat 2.5
        var location = tempoMap.GetMeasureAt(Rational.Create(21, 2));

        Assert.Equal(3, location.MeasureNumber);
        Assert.Equal(2.5, location.BeatInMeasure.ToDouble(), precision: 5);
    }

    [Fact]
    public void GetMeasureAt_WithTimeSignatureChange_CalculatesCorrectly()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[]
        {
            new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)),      // Measures 1-2: 8 beats
            new TimeSignatureChange(Rational.Create(8, 1), new TimeSignature(3, 4)) // Measure 3+: 3 beats each
        };
        var tempoMap = new TempoMap(tempos, timeSigs);

        // Beat 8 = start of measure 3 (first measure in 3/4 time)
        var location1 = tempoMap.GetMeasureAt(Rational.Create(8, 1));
        Assert.Equal(3, location1.MeasureNumber);
        Assert.Equal(Rational.Zero, location1.BeatInMeasure);

        // Beat 11 = measure 4, beat 0 (measure 3 = beats 8-10)
        var location2 = tempoMap.GetMeasureAt(Rational.Create(11, 1));
        Assert.Equal(4, location2.MeasureNumber);
        Assert.Equal(Rational.Zero, location2.BeatInMeasure);
    }

    [Fact]
    public void GetTempoAt_ReturnsCurrentTempo()
    {
        var tempos = new[]
        {
            new TempoChange(Rational.Zero, 120),
            new TempoChange(Rational.Create(4, 1), 80)
        };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        var tempoMap = new TempoMap(tempos, timeSigs);

        Assert.Equal(120, tempoMap.GetTempoAt(Rational.Create(2, 1)));
        Assert.Equal(80, tempoMap.GetTempoAt(Rational.Create(6, 1)));
    }

    [Fact]
    public void GetTimeSignatureAt_ReturnsCurrentTimeSignature()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[]
        {
            new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)),
            new TimeSignatureChange(Rational.Create(8, 1), new TimeSignature(3, 4))
        };
        var tempoMap = new TempoMap(tempos, timeSigs);

        var timeSig1 = tempoMap.GetTimeSignatureAt(Rational.Create(2, 1));
        Assert.Equal(4, timeSig1.Numerator);
        Assert.Equal(4, timeSig1.Denominator);

        var timeSig2 = tempoMap.GetTimeSignatureAt(Rational.Create(10, 1));
        Assert.Equal(3, timeSig2.Numerator);
        Assert.Equal(4, timeSig2.Denominator);
    }
}
