using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Analysis.Onset;

public class SpectralFluxOnsetDetectorTests : OnsetDetectorTestBase
{
    private const int SampleRate = DefaultSampleRate;

    [Fact]
    public void Constructor_InvalidFrameSize_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new SpectralFluxOnsetDetector(frameSize: 0));
        Assert.Throws<ArgumentException>(() => new SpectralFluxOnsetDetector(frameSize: -1));
        Assert.Throws<ArgumentException>(() => new SpectralFluxOnsetDetector(frameSize: 1000)); // Not power of 2
    }

    [Fact]
    public void Constructor_InvalidHopSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(hopSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(hopSize: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(frameSize: 2048, hopSize: 3000));
    }

    [Fact]
    public void Constructor_InvalidThreshold_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(threshold: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralFluxOnsetDetector(threshold: -0.1f));
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
        var detector = new SpectralFluxOnsetDetector(frameSize: 2048);
        var buffer = new float[1000]; // Smaller than frame size
        var onsets = detector.DetectOnsets(buffer, SampleRate);

        Assert.Empty(onsets);
    }

    [Fact]
    public void DetectOnsets_SilenceThenSound_DetectsOnsetAtBoundary()
    {
        var detector = new SpectralFluxOnsetDetector(threshold: 0.2f);

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
        var detector = new SpectralFluxOnsetDetector(threshold: 0.2f);

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
        var detector = new SpectralFluxOnsetDetector(threshold: 0.2f, minOnsetIntervalSeconds: 0.05f);

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
        var detector = new SpectralFluxOnsetDetector(
            threshold: 0.1f,
            minOnsetIntervalSeconds: minInterval);

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
        var sensitiveDetector = new SpectralFluxOnsetDetector(threshold: 0.1f);
        var sensitiveOnsets = sensitiveDetector.DetectOnsets(buffer, SampleRate);

        // High threshold = less sensitive
        var strictDetector = new SpectralFluxOnsetDetector(threshold: 1.0f);
        var strictOnsets = strictDetector.DetectOnsets(buffer, SampleRate);

        // Sensitive detector should find more or equal onsets
        Assert.True(sensitiveOnsets.Length >= strictOnsets.Length,
            $"Sensitive: {sensitiveOnsets.Length}, Strict: {strictOnsets.Length}");
    }

    [Fact]
    public void DetectOnsets_ClickTrack_DetectsAllClicks()
    {
        var detector = new SpectralFluxOnsetDetector(threshold: 0.15f, minOnsetIntervalSeconds: 0.05f);

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
        var detector = new SpectralFluxOnsetDetector(threshold: 0.2f);
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
        var detector = new SpectralFluxOnsetDetector(threshold: 0.25f, minOnsetIntervalSeconds: 0.05f);
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
        var detector = new SpectralFluxOnsetDetector(threshold: 0.2f);

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
}
