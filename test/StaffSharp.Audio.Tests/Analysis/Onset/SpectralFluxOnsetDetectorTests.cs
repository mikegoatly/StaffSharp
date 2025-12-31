using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Analysis.Onset;

public class SpectralFluxOnsetDetectorTests : OnsetDetectorTestBase
{
    private const int SampleRate = DefaultSampleRate;

    [Fact]
    public void Constructor_InvalidFrameSize_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { FrameSize = 0 }));
        Assert.Throws<ArgumentException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { FrameSize = -1 }));
        Assert.Throws<ArgumentException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { FrameSize = 1000 })); // Not power of 2
    }

    [Fact]
    public void Constructor_InvalidHopSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { HopSize = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { HopSize = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { FrameSize = 2048, HopSize = 3000 }));
    }

    [Fact]
    public void Constructor_InvalidThreshold_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = -0.1f }));
    }

    [Fact]
    public void DetectOnsets_EmptyBuffer_ReturnsEmpty()
    {
        var detector = new SpectralFluxOnsetDetector();
        var onsets = detector.DetectOnsets(ReadOnlySpan<float>.Empty, SampleRate);

        Assert.Empty(onsets);
    }

    [Fact]
    public void DetectOnsets_TooSmallBuffer_ReturnsEmpty()
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { FrameSize = 2048 });
        var buffer = new float[1000]; // Smaller than frame size
        var onsets = detector.DetectOnsets(buffer, SampleRate);

        Assert.Empty(onsets);
    }

    [Fact]
    public void DetectOnsets_SilenceThenSound_DetectsOnsetAtBoundary()
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.2f });

        // Pure silence followed by sound creates a clear onset
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .AtTime(0.2).WithAttack(0.01).AddSine(440.0, durationSeconds: 0.3)
            .Build();

        var onsets = detector.DetectOnsets(buffer, SampleRate);

        // Should detect the transition from silence to sound
        Assert.NotEmpty(onsets);
        AssertOnsetNear(onsets, 0.2);
    }

    [Fact]
    public void DetectOnsets_SingleNoteWithOnset_DetectsOnset()
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.2f });

        // Silence, then note with attack
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.7)
            .AtTime(0.1).WithAttack(0.01).AddSine(440.0, durationSeconds: 0.5)
            .Build();

        var onsets = detector.DetectOnsets(buffer, SampleRate);

        AssertMinimumOnsets(onsets, 1);
        AssertOnsetNear(onsets, 0.1);
    }

    [Fact]
    public void DetectOnsets_MultipleNotes_DetectsMultipleOnsets()
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.2f, MinOnsetIntervalSeconds = 0.05f });

        // Create buffer with multiple distinct note onsets
        var noteOnsetTimes = new[] { 0.1, 0.3, 0.5, 0.7 };
        var frequencies = new[] { 440.0, 523.25, 659.25, 783.99 }; // A4, C5, E5, G5

        var builder = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0);

        for (int i = 0; i < noteOnsetTimes.Length; i++)
        {
            builder.AtTime(noteOnsetTimes[i]).WithAttack(0.01).AddSine(frequencies[i], durationSeconds: 0.15);
        }

        var buffer = builder.Build();
        var onsets = detector.DetectOnsets(buffer, SampleRate);

        AssertMinimumOnsets(onsets, 3);
        AssertAllOnsetsDetected(onsets, noteOnsetTimes);
    }

    [Fact]
    public void DetectOnsets_MinIntervalFilter_EnforcesMinimumGap()
    {
        var minInterval = 0.1f;
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions
        {
            Threshold = 0.1f,
            MinOnsetIntervalSeconds = minInterval
        });

        // Try to create very close onsets
        var onsetTimes = new[] { 0.1, 0.12, 0.14, 0.3 };
        var builder = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.6);

        foreach (var time in onsetTimes)
        {
            builder.AtTime(time).WithAttack(0.01).AddSine(440.0, durationSeconds: 0.05);
        }

        var buffer = builder.Build();
        var onsets = detector.DetectOnsets(buffer, SampleRate);

        AssertMinimumInterval(onsets, minInterval, tolerancePercent: 0.1);
    }

    [Fact]
    public void DetectOnsets_DifferentThresholds_AffectsSensitivity()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .AtTime(0.1).WithAttack(0.01).AddSine(440.0, durationSeconds: 0.3)
            .Build();

        // Low threshold = more sensitive
        var sensitiveDetector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.1f });
        var sensitiveOnsets = sensitiveDetector.DetectOnsets(buffer, SampleRate);

        // High threshold = less sensitive
        var strictDetector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 1.0f });
        var strictOnsets = strictDetector.DetectOnsets(buffer, SampleRate);

        // Sensitive detector should find more or equal onsets
        Assert.True(sensitiveOnsets.Length >= strictOnsets.Length,
            $"Sensitive: {sensitiveOnsets.Length}, Strict: {strictOnsets.Length}");
    }

    [Fact]
    public void DetectOnsets_ClickTrack_DetectsAllClicks()
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.15f, MinOnsetIntervalSeconds = 0.05f });

        // Generate click track: brief impulses every 0.2s
        var clickTimes = new[] { 0.1, 0.3, 0.5, 0.7, 0.9 };

        var builder = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.2);

        foreach (var clickTime in clickTimes)
        {
            builder.AtTime(clickTime).AddImpulse();
        }

        var buffer = builder.Build();
        var onsets = detector.DetectOnsets(buffer, SampleRate);

        AssertMinimumOnsets(onsets, clickTimes.Length - 1);
    }

    [Theory]
    [InlineData(0.05)]  // Very clean: SNR ~20:1
    [InlineData(0.15)]  // Clean: SNR ~6.6:1
    [InlineData(0.3)]   // Moderate: SNR ~3.3:1
    public void DetectOnsets_NoteWithVaryingNoise_StillDetects(double noiseAmplitude)
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.2f });
        var onsetTime = 0.1;

        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .AtTime(onsetTime).WithAttack(0.01).AddSine(440.0, durationSeconds: 0.3)
            .AddNoise(amplitude: noiseAmplitude, seed: 42)
            .Build();

        var onsets = detector.DetectOnsets(buffer, SampleRate);

        // Should still detect onset even with noise
        AssertMinimumOnsets(onsets, 1);
        AssertOnsetNear(onsets, onsetTime, toleranceSeconds: 0.1);
    }

    [Theory]
    [InlineData(0.05)]  // Very clean
    [InlineData(0.15)]  // Clean
    [InlineData(0.3)]   // Moderate
    public void DetectOnsets_MultipleNotesWithNoise_DetectsMostOnsets(double noiseAmplitude)
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.25f, MinOnsetIntervalSeconds = 0.05f });
        var noteOnsetTimes = new[] { 0.1, 0.3, 0.5 };

        var builder = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.8)
            .AddNoise(amplitude: noiseAmplitude, seed: 42);

        foreach (var time in noteOnsetTimes)
        {
            builder.AtTime(time).WithAttack(0.01).AddSine(440.0, durationSeconds: 0.15);
        }

        var buffer = builder.Build();
        var onsets = detector.DetectOnsets(buffer, SampleRate);

        // With noise, we might not detect all onsets, but should get most
        var expectedMinimum = noiseAmplitude < 0.2 ? noteOnsetTimes.Length - 1 : noteOnsetTimes.Length - 2;
        AssertMinimumOnsets(onsets, expectedMinimum);
    }

    [Fact]
    public void DetectOnsets_HighNoise_MayMissOnsets()
    {
        var detector = new SpectralFluxOnsetDetector(new OnsetDetectionOptions { Threshold = 0.2f });

        // Very high noise (SNR ~1:1) may cause missed detections
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .AtTime(0.1).WithAttack(0.01).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.3)
            .AddNoise(amplitude: 0.5, seed: 42)
            .Build();

        var onsets = detector.DetectOnsets(buffer, SampleRate);

        // With high noise, onset detection becomes unreliable
        // This test documents the limitation rather than requiring detection
        // Either no onset detected or imprecise timing is acceptable
        if (onsets.Length > 0)
        {
            // If detected, should be somewhere near 0.1s (but tolerance is high)
            var nearOnset = onsets.Any(o => Math.Abs(o - 0.1) < 0.15);
            Assert.True(nearOnset || onsets.Length == 0,
                "With high noise, either no onset or rough detection is expected");
        }
    }

    [Fact]
    public void DetectOnsets_WithStartTimeOffset_AppliesOffsetCorrectly()
    {
        // CRITICAL TEST: Verify that start time offset preserves absolute timing
        // This is used when processing audio slices to maintain timing relative to original recording
        var detector = new SpectralFluxOnsetDetector();

        // Create audio with onset at 0.1s
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .AtTime(0.1).WithAttack(0.01).AddSine(440.0, amplitude: 0.8, durationSeconds: 0.3)
            .Build();

        // Simulate processing a slice that starts at 2.5 seconds into the original recording
        var startTimeOffset = 2.5;

        var onsetsWithOffset = detector.DetectOnsets(buffer, SampleRate, startTimeOffset);
        var onsetsWithoutOffset = detector.DetectOnsets(buffer, SampleRate, startTimeOffset: 0.0);

        // Should detect same number of onsets
        Assert.Equal(onsetsWithoutOffset.Length, onsetsWithOffset.Length);
        Assert.True(onsetsWithOffset.Length > 0, "Should detect at least one onset");

        // Each onset time should be offset by exactly startTimeOffset
        for (int i = 0; i < onsetsWithOffset.Length; i++)
        {
            var expectedTime = onsetsWithoutOffset[i] + startTimeOffset;
            Assert.Equal(expectedTime, onsetsWithOffset[i], precision: 6);
        }

        // First onset should be at ~2.6s (0.1s onset + 2.5s offset)
        Assert.InRange(onsetsWithOffset[0], 2.55, 2.65);
    }

    [Fact]
    public void DetectOnsets_WithZeroOffset_BehavesNormally()
    {
        var detector = new SpectralFluxOnsetDetector();

        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .AtTime(0.1).WithAttack(0.01).AddSine(440.0, amplitude: 0.8, durationSeconds: 0.3)
            .Build();

        // Explicit zero offset should be identical to omitting the parameter
        var onsetsExplicitZero = detector.DetectOnsets(buffer, SampleRate, startTimeOffset: 0.0);
        var onsetsDefault = detector.DetectOnsets(buffer, SampleRate);

        Assert.Equal(onsetsDefault.Length, onsetsExplicitZero.Length);
        for (int i = 0; i < onsetsDefault.Length; i++)
        {
            Assert.Equal(onsetsDefault[i], onsetsExplicitZero[i], precision: 10);
        }
    }
}
