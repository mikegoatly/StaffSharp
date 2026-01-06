using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Analysis.Pitch;

public class PyinPitchDetectorTests : PitchDetectorTestBase
{
    private const int SampleRate = DefaultSampleRate;

    [Fact]
    public void Constructor_InvalidFrequencyRange_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new PyinPitchDetector(new PitchDetectionOptions { MinFrequency = 1000, MaxFrequency = 100 }));
        Assert.Throws<ArgumentException>(() => new PyinPitchDetector(new PitchDetectionOptions { MinFrequency = 0, MaxFrequency = 1000 }));
        Assert.Throws<ArgumentException>(() => new PyinPitchDetector(new PitchDetectionOptions { MinFrequency = -100, MaxFrequency = 1000 }));
    }

    [Fact]
    public void Constructor_InvalidThreshold_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PyinPitchDetector(new PitchDetectionOptions { Threshold = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PyinPitchDetector(new PitchDetectionOptions { Threshold = 1.0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PyinPitchDetector(new PitchDetectionOptions { Threshold = -0.1f }));
    }

    [Fact]
    public void DetectPitch_EmptyBuffer_ReturnsDefault()
    {
        var detector = new PyinPitchDetector();
        var result = detector.DetectPitch(ReadOnlySpan<float>.Empty, SampleRate);

        Assert.False(result.IsPitched);
        Assert.Equal(0, result.FrequencyHz);
    }

    [Fact]
    public void DetectPitch_PureSineWave440Hz_DetectsCorrectly()
    {
        var detector = new PyinPitchDetector();
        var buffer = AudioSignalBuilder.Sine(440.0, 0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertPitchFrequency(result, 440.0, toleranceHz: 10.0);
        AssertConfidence(result, 0.5f);
        
        // pYIN should find multiple candidates
        Assert.True(result.Candidates?.Count > 0, "pYIN should return at least one candidate");
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
        var detector = new PyinPitchDetector();
        var buffer = AudioSignalBuilder.Sine(frequency, 0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        AssertPitchFrequencyPercent(result, frequency, tolerancePercent: 3.0);
    }

    [Fact]
    public void DetectPitch_HarmonicRichSignal_DetectsFundamental()
    {
        // Generate signal with fundamental + harmonics (simulates piano-like timbre)
        var fundamental = 220.0;
        var buffer = AudioSignalBuilder.Harmonics(fundamental, harmonicCount: 5, duration: 0.1, sampleRate: SampleRate);

        var detector = new PyinPitchDetector();
        var result = detector.DetectPitch(buffer, SampleRate);

        // pYIN should detect fundamental, not harmonics - this is the key test for octave error correction
        AssertPitchFrequencyPercent(result, fundamental, tolerancePercent: 5.0);
        
        // Should have high voicing probability for strong pitched signal
        Assert.True(result.VoicingProbability > 0.8f, $"Expected high voicing probability, got {result.VoicingProbability}");
    }

    [Fact]
    public void DetectPitch_MultipleCandidates_SortedByProbability()
    {
        var fundamental = 220.0;
        var buffer = AudioSignalBuilder.Harmonics(fundamental, harmonicCount: 3, duration: 0.1, sampleRate: SampleRate);

        var detector = new PyinPitchDetector();
        var result = detector.DetectPitch(buffer, SampleRate);

        // Should have multiple candidates
        Assert.True(result.Candidates?.Count > 1, "Expected multiple pitch candidates");
        
        // Candidates should be sorted by probability (highest first)
        for (int i = 0; i < result.Candidates.Count - 1; i++)
        {
            Assert.True(result.Candidates[i].Probability >= result.Candidates[i + 1].Probability,
                $"Candidates not sorted: {result.Candidates[i].Probability} < {result.Candidates[i + 1].Probability}");
        }
        
        // Best candidate should be the fundamental (not a harmonic)
        var bestFreq = result.Candidates[0].FrequencyHz;
        var errorPercent = Math.Abs(bestFreq - fundamental) / fundamental * 100;
        Assert.True(errorPercent < 10.0, 
            $"Best candidate {bestFreq:F1} Hz is not close to fundamental {fundamental} Hz (error: {errorPercent:F1}%)");
    }

    [Fact]
    public void DetectPitch_Noise_ReturnsLowConfidenceOrNoPitch()
    {
        var detector = new PyinPitchDetector();
        var buffer = AudioSignalBuilder.Noise(0.1, SampleRate);

        var result = detector.DetectPitch(buffer, SampleRate);

        // Should have low voicing probability for noise
        Assert.True(result.VoicingProbability < 0.5f, $"Expected low voicing probability for noise, got {result.VoicingProbability}");
    }
}
