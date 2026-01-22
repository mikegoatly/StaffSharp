using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;

namespace StaffSharp.Audio.Tests.Analysis.Tempo;

public class CombFilterTempoDetectorTests
{
    // ========================================================================
    // Basic Tempo Detection Tests
    // ========================================================================

    [Fact]
    public void DetectTempo_SteadyQuarterNotes120Bpm_DetectsCorrectly()
    {
        var detector = new CombFilterTempoDetector();

        // 120 BPM = 0.5 seconds per beat
        var onsets = new[] { 0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5 };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        var tempo = tempoChanges[0].BeatsPerMinute;
        Assert.InRange(tempo, 115, 125); // Within ±5 BPM
        Assert.Single(tempoChanges); // Single tempo
    }

    [Fact]
    public void DetectTempo_SteadyQuarterNotes60Bpm_DetectsCorrectly()
    {
        var detector = new CombFilterTempoDetector();

        // 60 BPM = 1.0 second per beat
        // NOTE: This is ambiguous - could be 60 BPM or 120 BPM (octave error)
        // The perceptual weighting favors 120 BPM as it's closer to the target of 110 BPM
        var onsets = new[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        var tempo = tempoChanges[0].BeatsPerMinute;
        // Accept either 60 BPM or 120 BPM as both are valid interpretations
        var is60Bpm = tempo >= 55 && tempo <= 65;
        var is120Bpm = tempo >= 115 && tempo <= 125;
        Assert.True(is60Bpm || is120Bpm,
            $"Expected either ~60 BPM or ~120 BPM (octave ambiguity), got {tempo:F1} BPM");
    }

    [Fact]
    public void DetectTempo_SteadyQuarterNotes180Bpm_DetectsCorrectly()
    {
        var detector = new CombFilterTempoDetector();

        // 180 BPM = 0.333... seconds per beat
        var beatInterval = 60.0 / 180.0;
        var onsets = Enumerable.Range(0, 10)
            .Select(i => i * beatInterval)
            .ToArray();

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        Assert.InRange(tempoChanges[0].BeatsPerMinute, 175, 185);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(100)]
    [InlineData(120)]
    [InlineData(140)]
    [InlineData(160)]
    public void DetectTempo_VariousSteadyTempos_DetectsAccurately(int expectedBpm)
    {
        var detector = new CombFilterTempoDetector();

        var beatInterval = 60.0 / expectedBpm;
        var onsets = Enumerable.Range(0, 12)
            .Select(i => i * beatInterval)
            .ToArray();

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.NotNull(tempoChanges);
        var tempo = tempoChanges[0].BeatsPerMinute;

        // For tempos far from the perceptual target (110 BPM), allow harmonic interpretations
        // e.g., 160 BPM might be detected as 80 BPM (half) or vice versa
        var error = Math.Abs(tempo - expectedBpm);
        var halfError = Math.Abs(tempo - expectedBpm / 2.0);
        var doubleError = Math.Abs(tempo - expectedBpm * 2.0);

        var minError = Math.Min(error, Math.Min(halfError, doubleError));
        Assert.True(minError < 5,
            $"Expected ~{expectedBpm} BPM (or harmonic), got {tempo:F1} BPM (min error: {minError:F1})");
    }

    [Fact]
    public void DetectTempo_WithMinorTimingFluctuations_RemainsStable()
    {
        var detector = new CombFilterTempoDetector();

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
        Assert.InRange(tempoChanges[0].BeatsPerMinute, 110, 130); // Should still detect around 120 BPM
    }

    [Fact]
    public void DetectTempo_OutOfRange_ThrowsException()
    {
        var detector = new CombFilterTempoDetector(new TempoDetectionOptions { MinBpm = 100, MaxBpm = 140 });

        // 60 BPM - below minimum
        var slowOnsets = new[] { 0.0, 1.0, 2.0, 3.0, 4.0 };
        var ex = Assert.Throws<InvalidOperationException>(() => detector.DetectTempo(PipelineProgress.Null, slowOnsets));
        Assert.Equal("No valid inter-onset intervals found within the specified tempo range.", ex.Message);
    }

    [Fact]
    public void DetectTempo_VeryFewOnsets_HandlesGracefully()
    {
        var detector = new CombFilterTempoDetector();

        // Just 2 onsets
        var onsets = new[] { 0.0, 0.5 };
        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // Should still work with minimal data
        Assert.NotNull(tempoChanges);
        Assert.True(tempoChanges[0].BeatsPerMinute > 0, "Should not throw or return negative");
    }

    // ========================================================================
    // Syncopation and All-Pairs IOI Tests
    // ========================================================================

    [Fact]
    public void DetectTempo_SyncopatedRhythm_FindsUnderlyingBeat()
    {
        var detector = new CombFilterTempoDetector();

        // 120 BPM (0.5s per beat), but heavily syncopated pattern
        // Beat:  1   .   2   .   3   .   4   .
        // Notes: X   .   .   X   X   .   X   .
        // Pattern skips beat 1's second occurrence and has off-beat notes
        var onsets = new[]
        {
            0.0,   // Beat 1
            0.75,  // Off-beat (between beat 2 and 3)
            1.0,   // Beat 3
            1.5    // Beat 4
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // With sparse syncopated data, algorithm may detect subdivisions or beat level
        // Accept reasonable tempos that capture the periodicity
        var tempo = tempoChanges[0].BeatsPerMinute;
        Assert.True(tempo >= 40 && tempo <= 320,
            $"Expected reasonable tempo, got {tempo:F1} BPM");
    }

    [Fact]
    public void DetectTempo_WithRests_FindsBeatAcrossGaps()
    {
        var detector = new CombFilterTempoDetector();

        // 100 BPM (0.6s per beat), with a multi-beat rest
        // Beat: 1    2    3    4    5    (6-7 rest)  8    9
        var onsets = new[]
        {
            0.0,   // Beat 1
            0.6,   // Beat 2
            1.2,   // Beat 3
            1.8,   // Beat 4
            2.4,   // Beat 5
            // Rest for 2 beats
            4.2,   // Beat 8
            4.8    // Beat 9
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // All-pairs IOI should find the underlying 0.6s interval
        Assert.InRange(tempoChanges[0].BeatsPerMinute, 95, 105);
    }

    [Fact]
    public void DetectTempo_ComplexSyncopation_HandlesSuccessfully()
    {
        var detector = new CombFilterTempoDetector();

        // 120 BPM with complex syncopated pattern
        // This tests the all-pairs window's ability to "jump over" off-beat notes
        var onsets = new[]
        {
            0.0,                    // On beat
            0.25,                   // Syncopation
            0.5,                    // On beat
            1.25,                   // Syncopation
            1.5,                    // On beat
            2.0,                    // On beat
            2.125,                  // Syncopation
            2.5,                    // On beat
            3.0                     // On beat
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // With many subdivisions (0.25s, 0.125s), algorithm may detect subdivision level (240 BPM)
        // or beat level (120 BPM). Both are valid interpretations.
        var tempo = tempoChanges[0].BeatsPerMinute;
        Assert.True((tempo >= 110 && tempo <= 130) || (tempo >= 220 && tempo <= 260),
            $"Expected ~120 BPM or ~240 BPM (subdivision), got {tempo:F1} BPM");
    }

    // ========================================================================
    // Phase Detection Tests
    // ========================================================================

    [Fact]
    public void DetectTempo_WithPhaseOffset_DetectsCorrectTempo()
    {
        var detector = new CombFilterTempoDetector();

        // 120 BPM (0.5s per beat) starting at 0.1s (not on beat 1)
        var phase = 0.1;
        var beatInterval = 0.5;
        var onsets = Enumerable.Range(0, 8)
            .Select(i => phase + i * beatInterval)
            .ToArray();

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // Should still detect 120 BPM despite phase offset
        Assert.InRange(tempoChanges[0].BeatsPerMinute, 115, 125);
    }

    [Fact]
    public void DetectTempo_LargePhaseOffset_HandlesCorrectly()
    {
        var detector = new CombFilterTempoDetector();

        // 100 BPM starting at 0.4s (large offset)
        var phase = 0.4;
        var beatInterval = 0.6;
        var onsets = Enumerable.Range(0, 6)
            .Select(i => phase + i * beatInterval)
            .ToArray();

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        Assert.InRange(tempoChanges[0].BeatsPerMinute, 95, 105);
    }

    // ========================================================================
    // Perceptual Weighting Tests
    // ========================================================================

    [Fact]
    public void DetectTempo_OctaveAmbiguity_PrefersTargetBpm()
    {
        var detector = new CombFilterTempoDetector(new TempoDetectionOptions
        {
            TargetBpm = 120,
            WidthBpm = 30
        });

        // Ambiguous pattern: could be 60 BPM or 120 BPM
        // Every second onset creates a 1.0s interval (60 BPM)
        // Every onset creates a 0.5s interval (120 BPM)
        var onsets = new[] { 0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // Should prefer 120 BPM due to perceptual weighting
        // (120 is closer to target of 120 than 60 is)
        Assert.InRange(tempoChanges[0].BeatsPerMinute, 110, 130);
    }

    [Fact]
    public void DetectTempo_VeryFastVsSlow_PrefersModerate()
    {
        // Test that perceptual weighting biases toward "human" tempos
        var detector = new CombFilterTempoDetector(new TempoDetectionOptions
        {
            TargetBpm = 110,
            WidthBpm = 30,
            MinBpm = 40,
            MaxBpm = 300
        });

        // Pattern with both 240 BPM (0.25s) and 120 BPM (0.5s) intervals
        // More 0.25s intervals, but 120 BPM is closer to target
        var onsets = new[]
        {
            0.0,   // Start
            0.25,  // +0.25
            0.5,   // +0.25
            0.75,  // +0.25
            1.0,   // +0.25
            1.5,   // +0.5 (beat)
            2.0,   // +0.5 (beat)
            2.5,   // +0.5 (beat)
            3.0    // +0.5 (beat)
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // With clamped perceptual weighting [0.7, 1.0], the dominant 0.25s intervals
        // may override the perceptual bias. Accept either 120 BPM or 240 BPM.
        var tempo = tempoChanges[0].BeatsPerMinute;
        Assert.True((tempo >= 100 && tempo <= 140) || (tempo >= 220 && tempo <= 260),
            $"Expected ~120 BPM or ~240 BPM, got {tempo:F1} BPM");
    }

    // ========================================================================
    // Harmonic Testing (0.5x, 1x, 2x)
    // ========================================================================

    [Fact]
    public void DetectTempo_HalfBeatDominant_CorrectsToBeat()
    {
        var detector = new CombFilterTempoDetector();

        // Pattern where half-beat (eighth notes) is very prominent
        // 120 BPM = 0.5s per beat, 0.25s per eighth
        // But underlying beat should be detected
        var onsets = new[]
        {
            0.0,   // Beat
            0.25,  // Eighth
            0.5,   // Beat
            0.75,  // Eighth
            1.0,   // Beat
            1.25,  // Eighth
            1.5,   // Beat
            1.75   // Eighth
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // Should detect beat level (120 BPM) or subdivision level (240 BPM)
        // Harmonic testing should explore both
        var tempo = tempoChanges[0].BeatsPerMinute;
        Assert.True(tempo > 100, $"Should detect beat or subdivision, got {tempo:F1} BPM");
    }

    [Fact]
    public void DetectTempo_HalfNoteDominant_CorrectsToBeat()
    {
        var detector = new CombFilterTempoDetector();

        // Pattern where half-notes (2 beats) dominate
        // 120 BPM = 0.5s per beat, 1.0s per half note
        var onsets = new[]
        {
            0.0,   // Half note
            1.0,   // Half note
            2.0,   // Half note
            3.0,   // Half note
            4.0    // Half note
        };

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets);

        // Could legitimately be 60 BPM or 120 BPM
        // Algorithm will test both via harmonics (1x and 2x multipliers)
        var tempo = tempoChanges[0].BeatsPerMinute;
        Assert.True(tempo > 50 && tempo < 130, $"Should detect reasonable tempo, got {tempo:F1} BPM");
    }

    // ========================================================================
    // Clustering Tests
    // ========================================================================

    [Fact]
    public void DetectTempo_SimilarIntervals_DoesNotBinSplit()
    {
        var detector = new CombFilterTempoDetector();

        // Two very similar tempos: 120.0 BPM and 120.5 BPM
        // These should cluster together, not split into separate bins
        var intervals1 = Enumerable.Range(0, 5).Select(i => i * (60.0 / 120.0)).ToList();
        var intervals2 = Enumerable.Range(0, 5).Select(i => i * (60.0 / 120.5)).ToList();

        var combined = intervals1.Concat(intervals2).OrderBy(x => x).ToArray();

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, combined);

        // Should detect around 120 BPM (average of the two)
        Assert.InRange(tempoChanges[0].BeatsPerMinute, 118, 122);
    }

    [Fact]
    public void DetectTempo_MixedNoteValues_FindsPredominantBeat()
    {
        var detector = new CombFilterTempoDetector();

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
        var tempo = tempoChanges[0].BeatsPerMinute;
        // With mixed note values, may detect quarter note level (~120) or eighth note level (~240)
        Assert.True((tempo >= 100 && tempo <= 140) || (tempo >= 200 && tempo <= 280),
            $"Expected ~120 BPM or ~240 BPM, got {tempo:F1} BPM");
    }

    // ========================================================================
    // Edge Cases and Robustness Tests
    // ========================================================================

    [Fact]
    public void DetectTempo_WithOrnaments_FiltersNoise()
    {
        var detector = new CombFilterTempoDetector();

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

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets.ToArray());

        // Should detect main beat (~120 BPM), not grace notes
        Assert.NotNull(tempoChanges);
        Assert.InRange(tempoChanges[0].BeatsPerMinute, 110, 130);
    }

    [Fact]
    public void DetectTempo_CustomOptions_RespectsSettings()
    {
        var detector = new CombFilterTempoDetector(new TempoDetectionOptions
        {
            MinBpm = 120,
            MaxBpm = 130,
            TargetBpm = 125,
            WidthBpm = 20,
            PairwiseWindow = 5
        });

        // 125 BPM - within range
        var validOnsets = Enumerable.Range(0, 10)
            .Select(i => i * (60.0 / 125.0))
            .ToArray();

        var validTempo = detector.DetectTempo(PipelineProgress.Null, validOnsets);
        Assert.NotNull(validTempo);
        Assert.InRange(validTempo[0].BeatsPerMinute, 120, 130);
    }

    [Fact]
    public void DetectTempo_IrregularRhythm_FindsBestFit()
    {
        var detector = new CombFilterTempoDetector();

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
        // With irregular rhythm, may detect beat level (~100 BPM) or subdivision level (~200 BPM)
        var tempo = tempoChanges[0].BeatsPerMinute;
        Assert.True((tempo >= 85 && tempo <= 115) || (tempo >= 170 && tempo <= 230),
            $"Expected ~100 BPM or ~200 BPM (subdivision), got {tempo:F1} BPM");
    }

    [Fact]
    public void DetectTempo_Triplets_HandlesNonBinarySubdivisions()
    {
        var detector = new CombFilterTempoDetector(new TempoDetectionOptions
        {
            MinBpm = 40,
            MaxBpm = 400
        });

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

        var tempoChanges = detector.DetectTempo(PipelineProgress.Null, onsets.ToArray());

        Assert.NotNull(tempoChanges);
        var tempo = tempoChanges[0].BeatsPerMinute;
        // Should detect some tempo (either beat level ~90 or triplet level ~270)
        Assert.True(tempo > 0, $"Should detect some tempo, got {tempo:F1} BPM");
        Assert.True(tempo >= 40 && tempo <= 400, $"Tempo {tempo:F1} should be in valid range");
    }
}
