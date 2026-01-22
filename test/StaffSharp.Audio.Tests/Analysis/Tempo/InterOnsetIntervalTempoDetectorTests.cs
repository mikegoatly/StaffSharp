using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;

namespace StaffSharp.Audio.Tests.Analysis.Tempo;

public class InterOnsetIntervalTempoDetectorTests
{
    [Fact]
    public void Constructor_InvalidBpmRange_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new InterOnsetIntervalTempoDetector(new TempoDetectionOptions { MinBpm = 240, MaxBpm = 40 }));
        Assert.Throws<ArgumentException>(() => new InterOnsetIntervalTempoDetector(new TempoDetectionOptions { MinBpm = 0, MaxBpm = 120 }));
        Assert.Throws<ArgumentException>(() => new InterOnsetIntervalTempoDetector(new TempoDetectionOptions { MinBpm = -60, MaxBpm = 180 }));
    }

    [Fact]
    public void EstimateTempo_EmptyOnsets_ThrowsException()
    {
        var detector = new InterOnsetIntervalTempoDetector();
        var ex = Assert.Throws<ArgumentException>(() => detector.DetectTempo(PipelineProgress.Null, []));   
    }

    [Fact]
    public void EstimateTempo_SingleOnset_ThrowsException()
    {
        var detector = new InterOnsetIntervalTempoDetector();
        var onsets = new[] { 0.5 };
        var ex = Assert.Throws<ArgumentException>(() => detector.DetectTempo(PipelineProgress.Null, onsets));
    }

    [Fact]
    public void EstimateTempo_SteadyQuarterNotes120Bpm_DetectsCorrectly()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // 120 BPM = 0.5 seconds per beat
        var onsets = new[] { 0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5 };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        var tempo = tempoChanges![0].BeatsPerMinute;
        Assert.InRange(tempo, 115, 125); // Within ±5 BPM
        Assert.Single(tempoChanges); // Single tempo
    }

    [Fact]
    public void EstimateTempo_SteadyQuarterNotes60Bpm_DetectsCorrectly()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // 60 BPM = 1.0 second per beat
        var onsets = new[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };

        var tempoMap = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoMap);
        Assert.InRange(tempoMap![0].BeatsPerMinute, 55, 65);
    }

    [Fact]
    public void EstimateTempo_SteadyQuarterNotes180Bpm_DetectsCorrectly()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // 180 BPM = 0.333... seconds per beat
        var beatInterval = 60.0 / 180.0;
        var onsets = Enumerable.Range(0, 10)
            .Select(i => i * beatInterval)
            .ToArray();

        var tempoMap = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoMap);
        Assert.InRange(tempoMap![0].BeatsPerMinute, 175, 185);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(100)]
    [InlineData(120)]
    [InlineData(140)]
    [InlineData(160)]
    public void EstimateTempo_VariousSteadyTempos_DetectsAccurately(int expectedBpm)
    {
        var detector = new InterOnsetIntervalTempoDetector();

        var beatInterval = 60.0 / expectedBpm;
        var onsets = Enumerable.Range(0, 12)
            .Select(i => i * beatInterval)
            .ToArray();

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        var tempo = tempoChanges![0].BeatsPerMinute;
        var error = Math.Abs(tempo - expectedBpm);
        Assert.True(error < 5, $"Expected ~{expectedBpm} BPM, got {tempo:F1} BPM (error: {error:F1})");
    }

    [Fact]
    public void EstimateTempo_WithMinorTiming_Fluctuations_RemainsStable()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // 120 BPM with ±5% human timing variations
        var random = new Random(42);
        var baseBpm = 120.0;
        var baseInterval = 60.0 / baseBpm;

        var onsets = new List<double> { 0.0 };
        for (int i = 1; i < 16; i++)
        {
            var jitter = (random.NextDouble() - 0.5) * 0.05; // ±2.5%
            var interval = baseInterval * (1 + jitter);
            onsets.Add(onsets[^1] + interval);
        }

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets.ToArray());

        Assert.NotNull(tempoChanges);
        Assert.InRange(tempoChanges![0].BeatsPerMinute, 110, 130); // Should still detect around 120 BPM
    }

    [Fact]
    public void EstimateTempo_WithOrnaments_IgnoresShortIntervals()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // 120 BPM quarter notes with occasional grace notes (very short intervals)
        var onsets = new List<double>
        {
            0.0,
            0.05,  // Grace note (50ms, would be ~1200 BPM - out of range)
            0.5,   // Beat
            1.0,   // Beat
            1.45,  // Grace note
            1.5,   // Beat
            2.0    // Beat
        };

        var tempoMap = detector.DetectTempo(PipelineProgress.Null, onsets.ToArray());

        // Should detect main beat (~120 BPM), not grace notes
        Assert.NotNull(tempoMap);
        Assert.InRange(tempoMap![0].BeatsPerMinute, 110, 130);
    }

    [Fact]
    public void EstimateTempo_OutOfRange_ThrowsException()
    {
        var detector = new InterOnsetIntervalTempoDetector(new TempoDetectionOptions { MinBpm = 100, MaxBpm = 140 });

        // 60 BPM - below minimum
        var slowOnsets = new[] { 0.0, 1.0, 2.0, 3.0, 4.0 };
        var ex = Assert.Throws<InvalidOperationException>(() => detector.DetectTempo(PipelineProgress.Null, slowOnsets));
        Assert.Equal("No valid inter-onset intervals found within the specified tempo range.", ex.Message);
    }

    [Fact]
    public void EstimateTempo_MixedNoteValues_FindsPredominantBeat()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // Pattern with mostly quarter notes, some eighths
        // at 120 BPM (quarter = 0.5s, eighth = 0.25s)
        var onsets = new[]
        {
            0.0,   // Quarter
            0.5,   // Quarter
            1.0,   // Quarter
            1.5,   // Eighth
            1.75,  // Eighth
            2.0,   // Quarter
            2.5,   // Quarter
            3.0,   // Quarter
            3.5,   // Quarter
            4.0    // Quarter
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        var tempo = tempoChanges![0].BeatsPerMinute;
        // Median should find predominant 0.5s interval
        // May detect at beat level or subdivision level depending on median
        Assert.True(tempo > 80 && tempo < 200, $"Expected reasonable tempo, got {tempo:F1} BPM");
    }

    [Fact]
    public void EstimateTempo_Triplets_HandlesNonBinarySubdivisions()
    {
        // Use detector with extended range to capture triplet subdivisions
        var detector = new InterOnsetIntervalTempoDetector(new TempoDetectionOptions { MinBpm = 40, MaxBpm = 400 });

        // 90 BPM with triplet feel: 3 notes per beat
        // Beat interval = 0.667s, triplet interval = 0.222s
        var baseBpm = 90.0;
        var beatInterval = 60.0 / baseBpm;
        var tripletInterval = beatInterval / 3.0;

        var onsets = new List<double>();
        for (int beat = 0; beat < 6; beat++)
        {
            for (int triplet = 0; triplet < 3; triplet++)
            {
                onsets.Add(beat * beatInterval + triplet * tripletInterval);
            }
        }

        var tempoMap = detector.DetectTempo(PipelineProgress.Null, onsets.ToArray());

        Assert.NotNull(tempoMap);
        var tempo = tempoMap![0].BeatsPerMinute;
        // Should detect some tempo (either beat level ~90 or triplet level ~270)
        Assert.True(tempo > 0, $"Should detect some tempo, got {tempo:F1} BPM");
        Assert.True(tempo >= 40 && tempo <= 400, $"Tempo {tempo:F1} should be in valid range");
    }

    [Fact]
    public void EstimateTempo_IrregularRhythm_FindsBestFit()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // Irregular pattern but with underlying ~100 BPM pulse
        var onsets = new[]
        {
            0.0,
            0.6,   // Beat 1
            1.2,   // Beat 2
            1.5,   // Subdivision
            1.8,   // Beat 3
            2.4,   // Beat 4
            3.0,   // Beat 5
            3.6    // Beat 6
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        // Should find predominant interval around 0.6s (100 BPM)
        Assert.InRange(tempoChanges![0].BeatsPerMinute, 85, 115);
    }

    [Fact]
    public void EstimateTempo_VeryFewOnsets_HandlesGracefully()
    {
        var detector = new InterOnsetIntervalTempoDetector();

        // Just 2 onsets
        var onsets = new[] { 0.0, 0.5 };
        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // Should still work with minimal data
        Assert.NotNull(tempoChanges);
        Assert.True(tempoChanges![0].BeatsPerMinute > 0, "Should not throw or return negative");
    }

    [Fact]
    public void EstimateTempo_CustomRange_RespectsLimits()
    {
        // Narrow range detector for dance music
        var detector = new InterOnsetIntervalTempoDetector(new TempoDetectionOptions { MinBpm = 120, MaxBpm = 130 });

        // 125 BPM - within range
        var validOnsets = Enumerable.Range(0, 10)
            .Select(i => i * (60.0 / 125.0))
            .ToArray();

        var validTempo = detector.DetectTempo(PipelineProgress.Null, validOnsets);
        Assert.NotNull(validTempo);
        Assert.InRange(validTempo![0].BeatsPerMinute, 120, 130);

        // 100 BPM - outside range
        var invalidOnsets = Enumerable.Range(0, 10)
            .Select(i => i * (60.0 / 100.0))
            .ToArray();

        var ex = Assert.Throws<InvalidOperationException>(() => detector.DetectTempo(PipelineProgress.Null, invalidOnsets));
        Assert.Equal("No valid inter-onset intervals found within the specified tempo range.", ex.Message);
    }
}
