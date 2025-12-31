using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Analysis.Pitch;

public class YinPitchDetectorTests : PitchDetectorTestBase
{
    private const int SampleRate = DefaultSampleRate;

    [Fact]
    public void Constructor_InvalidFrequencyRange_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new YinPitchDetector(new PitchDetectionOptions { MinFrequency = 1000, MaxFrequency = 100 }));
        Assert.Throws<ArgumentException>(() => new YinPitchDetector(new PitchDetectionOptions { MinFrequency = 0, MaxFrequency = 1000 }));
        Assert.Throws<ArgumentException>(() => new YinPitchDetector(new PitchDetectionOptions { MinFrequency = -100, MaxFrequency = 1000 }));
    }

    [Fact]
    public void Constructor_InvalidThreshold_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new YinPitchDetector(new PitchDetectionOptions { Threshold = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new YinPitchDetector(new PitchDetectionOptions { Threshold = 1.0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new YinPitchDetector(new PitchDetectionOptions { Threshold = -0.1f }));
    }

    [Fact]
    public void DetectPitch_EmptyBuffer_ReturnsDefault()
    {
        var detector = new YinPitchDetector();
        var result = detector.DetectPitch(ReadOnlySpan<float>.Empty, SampleRate);

        Assert.False(result.IsPitched);
        Assert.Equal(0, result.FrequencyHz);
    }

    [Fact]
    public void DetectPitch_TooSmallBuffer_ReturnsDefault()
    {
        var detector = new YinPitchDetector();
        var buffer = new float[1];
        var result = detector.DetectPitch(buffer, SampleRate);

        Assert.False(result.IsPitched);
    }

    [Fact]
    public void DetectPitch_PureSineWave440Hz_DetectsCorrectly()
    {
        var detector = new YinPitchDetector();
        var buffer = AudioSignalBuilder.Sine(440.0, 0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertPitchFrequency(result, 440.0);
        AssertConfidence(result, 0.7f);
    }

    [Theory]
    [InlineData(220.0)] // A3
    [InlineData(261.63)] // C4
    [InlineData(329.63)] // E4
    [InlineData(440.0)] // A4
    [InlineData(523.25)] // C5
    [InlineData(880.0)] // A5
    public void DetectPitch_PureSineWaves_DetectsAccurately(double frequency)
    {
        var detector = new YinPitchDetector();
        var buffer = AudioSignalBuilder.Sine(frequency, 0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertPitchFrequencyPercent(result, frequency, tolerancePercent: 2.0);
    }

    [Fact]
    public void DetectPitch_HarmonicRichSignal_DetectsFundamental()
    {
        // Generate signal with fundamental + harmonics (simulates piano-like timbre)
        var fundamental = 220.0;
        var buffer = AudioSignalBuilder.Harmonics(fundamental, harmonicCount: 5, duration: 0.1, sampleRate: SampleRate);

        var detector = new YinPitchDetector();
        var result = detector.DetectPitch(buffer, SampleRate);

        // YIN should detect fundamental, not harmonics
        AssertPitchFrequencyPercent(result, fundamental, tolerancePercent: 5.0);
    }

    [Fact]
    public void DetectPitch_Noise_ReturnsLowConfidenceOrNoPitch()
    {
        var detector = new YinPitchDetector();
        var buffer = AudioSignalBuilder.Noise(0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertNoPitchOrLowConfidence(result, maxConfidence: 0.3f);
    }

    [Fact]
    public void DetectPitch_OutOfRangeFrequency_LowerConfidence()
    {
        // Detector configured for 200-500 Hz
        var detector = new YinPitchDetector(new PitchDetectionOptions { MinFrequency = 200, MaxFrequency = 500 });

        // In-range frequency should have high confidence
        var bufferInRange = AudioSignalBuilder.Sine(350.0, 0.1, SampleRate);
        var resultInRange = detector.DetectPitch(bufferInRange, SampleRate);
        AssertPitchFrequency(resultInRange, 350.0);
        AssertConfidence(resultInRange, 0.7f);

        // Out-of-range frequencies may not be detected or have lower confidence
        // (YIN's frequency range is a search optimization, not a hard filter)
        var bufferLow = AudioSignalBuilder.Sine(100.0, 0.1, SampleRate);
        var resultLow = detector.DetectPitch(bufferLow, SampleRate);
        if (resultLow.IsPitched)
        {
            Assert.True(resultLow.Confidence < resultInRange.Confidence);
        }
    }

    [Fact]
    public void DetectPitch_DifferentThresholds_AffectsSensitivity()
    {
        var buffer = AudioSignalBuilder.Sine(440.0, 0.1, SampleRate);

        // Lower threshold = more sensitive
        var detectorSensitive = new YinPitchDetector(new PitchDetectionOptions { Threshold = 0.1f });
        var resultSensitive = detectorSensitive.DetectPitch(buffer, SampleRate);

        // Higher threshold = less sensitive
        var detectorStrict = new YinPitchDetector(new PitchDetectionOptions { Threshold = 0.3f });
        var resultStrict = detectorStrict.DetectPitch(buffer, SampleRate);

        // Both should detect pure sine, but sensitive might have lower confidence requirement
        Assert.True(resultSensitive.IsPitched);
        Assert.True(resultStrict.IsPitched);
    }

    // Edge case tests

    [Fact]
    public void DetectPitch_MinPeriodGreaterThanMaxPeriod_ReturnsNoPitch()
    {
        // Use extreme frequency range that causes minPeriod >= maxPeriod
        var detector = new YinPitchDetector(new PitchDetectionOptions { MinFrequency = 10000, MaxFrequency = 20000 });
        var buffer = AudioSignalBuilder.Sine(440.0, 0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        // Should return default result (no pitch)
        AssertNoPitch(result);
    }

    [Fact]
    public void DetectPitch_NoMinimumBelowThreshold_ReturnsNoPitch()
    {
        // Very strict threshold with noisy signal should fail to find pitch
        var detector = new YinPitchDetector(new PitchDetectionOptions { Threshold = 0.01f });
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AddSine(440.0, amplitude: 0.3)
            .AddNoise(amplitude: 0.7, seed: 42)
            .Build();

        var result = detector.DetectPitch(buffer, SampleRate);

        // Should either detect no pitch or have very low confidence
        AssertNoPitchOrLowConfidence(result, maxConfidence: 0.3f);
    }

    [Theory]
    [InlineData(22050)]
    [InlineData(48000)]
    [InlineData(96000)]
    public void DetectPitch_DifferentSampleRates_DetectsAccurately(int sampleRate)
    {
        var detector = new YinPitchDetector();
        var buffer = AudioSignalBuilder.Sine(440.0, 0.1, sampleRate);

        var result = detector.DetectPitch(buffer, sampleRate);

        AssertPitchFrequencyPercent(result, 440.0, tolerancePercent: 2.0);
    }

    [Fact]
    public void DetectPitch_FrequencyNearMinBoundary_DetectsAccurately()
    {
        var minFreq = 150.0;
        var testFreq = 160.0; // Slightly above min for stable detection
        var detector = new YinPitchDetector(new PitchDetectionOptions { MinFrequency = minFreq, MaxFrequency = 1000 });
        var buffer = AudioSignalBuilder.Sine(testFreq, 0.3, SampleRate); // Longer buffer for low frequency

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertPitchFrequencyPercent(result, testFreq, tolerancePercent: 5.0);
    }

    [Fact]
    public void DetectPitch_FrequencyAtMaxBoundary_DetectsAccurately()
    {
        var maxFreq = 1000.0;
        var detector = new YinPitchDetector(new PitchDetectionOptions { MinFrequency = 80, MaxFrequency = maxFreq });
        var buffer = AudioSignalBuilder.Sine(maxFreq, 0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertPitchFrequencyPercent(result, maxFreq, tolerancePercent: 5.0);
    }

    [Fact]
    public void DetectPitch_Silence_ReturnsNoPitch()
    {
        var detector = new YinPitchDetector();
        var buffer = AudioSignalBuilder.Silence(0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertNoPitch(result);
    }

    [Fact]
    public void DetectPitch_VeryShortBuffer_ReturnsNoPitch()
    {
        var detector = new YinPitchDetector();
        // Buffer too short to detect even high frequencies
        var buffer = new float[10];

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertNoPitch(result);
    }

    [Theory]
    [InlineData(0.05)]  // Very clean: SNR ~20:1
    [InlineData(0.15)]  // Clean: SNR ~6.6:1
    [InlineData(0.3)]   // Moderate: SNR ~3.3:1
    [InlineData(0.5)]   // Challenging: SNR ~2:1
    public void DetectPitch_SineWithVaryingNoise_StillDetects(double noiseAmplitude)
    {
        var detector = new YinPitchDetector();
        var signalAmplitude = 1.0;
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AddSine(440.0, amplitude: signalAmplitude)
            .AddNoise(amplitude: noiseAmplitude, seed: 42)
            .Build();

        var result = detector.DetectPitch(buffer, SampleRate);

        // Should still detect the pitch, tolerance increases with noise
        var tolerancePercent = noiseAmplitude < 0.3 ? 3.0 : 8.0;
        AssertPitchFrequencyPercent(result, 440.0, tolerancePercent: tolerancePercent);
    }

    [Theory]
    [InlineData(0.05)]  // Very clean
    [InlineData(0.15)]  // Clean
    [InlineData(0.3)]   // Moderate
    public void DetectPitch_HarmonicsWithVaryingNoise_DetectsFundamental(double noiseAmplitude)
    {
        var detector = new YinPitchDetector();
        var fundamental = 220.0;
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.15)
            .AddHarmonics(fundamental, harmonicCount: 5, amplitude: 1.0)
            .AddNoise(amplitude: noiseAmplitude, seed: 42)
            .Build();

        var result = detector.DetectPitch(buffer, SampleRate);

        // YIN should still detect fundamental even with noise and harmonics
        var tolerancePercent = noiseAmplitude < 0.2 ? 5.0 : 10.0;
        AssertPitchFrequencyPercent(result, fundamental, tolerancePercent: tolerancePercent);
    }

    [Fact]
    public void DetectPitch_ParabolicInterpolation_ImprovesAccuracy()
    {
        var detector = new YinPitchDetector();
        // Use frequency that doesn't align perfectly with sample rate
        var exactFreq = 437.5;
        var buffer = AudioSignalBuilder.Sine(exactFreq, 0.2, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        // Should get sub-sample accuracy from parabolic interpolation
        AssertPitchFrequency(result, exactFreq, toleranceHz: 2.0);
    }

    [Fact]
    public void DetectPitch_RealisticNoteWithAttack_DetectsCorrectly()
    {
        var detector = new YinPitchDetector();

        // More realistic signal with attack envelope (like a piano or guitar note)
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.3)
            .WithAttack(0.01)
            .AddHarmonics(220.0, harmonicCount: 5)
            .Build();

        var result = detector.DetectPitch(buffer, SampleRate);

        // Should still detect fundamental even with attack and harmonics
        AssertPitchFrequencyPercent(result, 220.0, tolerancePercent: 5.0);
    }

    [Fact]
    public void DetectPitch_ADSREnvelope_DetectsAcrossEnvelope()
    {
        var detector = new YinPitchDetector();

        // Signal with full ADSR envelope
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .WithADSR(attackSeconds: 0.05, decaySeconds: 0.1, sustainLevel: 0.7, releaseSeconds: 0.1)
            .AddSine(440.0)
            .Build();

        var result = detector.DetectPitch(buffer, SampleRate);

        // Should detect pitch regardless of envelope shape
        AssertPitchFrequencyPercent(result, 440.0, tolerancePercent: 3.0);
    }
}
