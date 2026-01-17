using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Pipeline;

namespace StaffSharp.Audio.Tests.Analysis.Boundaries;

public class EnergyBasedBoundaryDetectorTests
{
    private const int SampleRate = 44100;

    [Fact]
    public void Constructor_InvalidThreshold_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { ThresholdDb = 0.0f }));
        Assert.Throws<ArgumentException>(() =>
            new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { ThresholdDb = 10.0f }));
    }

    [Fact]
    public void Constructor_InvalidWindowSize_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { WindowSize = 0 }));
        Assert.Throws<ArgumentException>(() =>
            new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { WindowSize = -100 }));
    }

    [Fact]
    public void Constructor_InvalidMinContentSamples_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { MinContentSamples = 0 }));
        Assert.Throws<ArgumentException>(() =>
            new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { MinContentSamples = -100 }));
    }

    [Fact]
    public void DetectBoundaries_NullAudio_ThrowsException()
    {
        var detector = new EnergyBasedBoundaryDetector();
        Assert.Throws<ArgumentNullException>(() => detector.DetectBoundaries(PipelineProgress.Null, null!));
    }

    [Fact]
    public void DetectBoundaries_AllSilence_ReturnsNull()
    {
        var detector = new EnergyBasedBoundaryDetector();
        var audio = CreateSilence(SampleRate * 2); // 2 seconds of silence

        var result = detector.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.Null(result);
    }

    [Fact]
    public void DetectBoundaries_TooShort_ReturnsNull()
    {
        var detector = new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { MinContentSamples = 10000 });
        // Create audio shorter than minimum
        var audio = CreateAudioWithContent(
            leadingSilenceSamples: 1000,
            contentSamples: 5000, // Less than minContentSamples
            trailingSilenceSamples: 1000);

        var result = detector.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.Null(result); // Content too short
    }

    [Fact]
    public void DetectBoundaries_NoLeadingOrTrailingSilence_DetectsFullBuffer()
    {
        var detector = new EnergyBasedBoundaryDetector();
        var audio = CreateTone(SampleRate, 440.0f); // 1 second of 440Hz tone

        var result = detector.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.NotNull(result);
        Assert.Equal(0, result!.StartSample);
        Assert.Equal(SampleRate, result.EndSample);
        Assert.Equal(TimeSpan.Zero, result.LeadingSilence);
        Assert.Equal(TimeSpan.Zero, result.TrailingSilence);
    }

    [Fact]
    public void DetectBoundaries_WithLeadingSilence_DetectsCorrectStart()
    {
        var detector = new EnergyBasedBoundaryDetector();
        var leadingSamples = SampleRate / 2; // 0.5 seconds
        var contentSamples = SampleRate; // 1 second

        var audio = CreateAudioWithContent(
            leadingSilenceSamples: leadingSamples,
            contentSamples: contentSamples,
            trailingSilenceSamples: 0);

        var result = detector.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.NotNull(result);
        // Should detect start near the actual content start (within window tolerance)
        Assert.InRange(result!.StartSample, leadingSamples - 2048, leadingSamples + 2048);
        Assert.InRange(result.LeadingSilence.TotalSeconds, 0.4, 0.6); // ~0.5 seconds
    }

    [Fact]
    public void DetectBoundaries_WithTrailingSilence_DetectsCorrectEnd()
    {
        var detector = new EnergyBasedBoundaryDetector();
        var contentSamples = SampleRate; // 1 second
        var trailingSamples = SampleRate / 2; // 0.5 seconds

        var audio = CreateAudioWithContent(
            leadingSilenceSamples: 0,
            contentSamples: contentSamples,
            trailingSilenceSamples: trailingSamples);

        var result = detector.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.NotNull(result);
        // Should detect end near the actual content end (within window tolerance)
        Assert.InRange(result!.EndSample, contentSamples - 2048, contentSamples + 2048);
        Assert.InRange(result.TrailingSilence.TotalSeconds, 0.4, 0.6); // ~0.5 seconds
    }

    [Fact]
    public void DetectBoundaries_WithBothSilences_DetectsCorrectBoundaries()
    {
        var detector = new EnergyBasedBoundaryDetector();
        var leadingSamples = SampleRate; // 1 second
        var contentSamples = SampleRate * 2; // 2 seconds
        var trailingSamples = SampleRate; // 1 second

        var audio = CreateAudioWithContent(
            leadingSilenceSamples: leadingSamples,
            contentSamples: contentSamples,
            trailingSilenceSamples: trailingSamples);

        var result = detector.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.NotNull(result);

        // Verify boundaries (within window tolerance)
        Assert.InRange(result!.StartSample, leadingSamples - 2048, leadingSamples + 2048);
        Assert.InRange(result.EndSample,
            leadingSamples + contentSamples - 2048,
            leadingSamples + contentSamples + 2048);

        // Verify silence durations
        Assert.InRange(result.LeadingSilence.TotalSeconds, 0.9, 1.1); // ~1 second
        Assert.InRange(result.TrailingSilence.TotalSeconds, 0.9, 1.1); // ~1 second

        // Verify content duration
        Assert.InRange(result.ContentDuration.TotalSeconds, 1.8, 2.2); // ~2 seconds

        // Verify total duration
        Assert.InRange(result.TotalDuration.TotalSeconds, 3.8, 4.2); // ~4 seconds
    }

    [Fact]
    public void DetectBoundaries_PreservesAbsoluteTiming()
    {
        // CRITICAL TEST: Verify that boundaries are relative to original buffer start,
        // not relative to where content begins
        var detector = new EnergyBasedBoundaryDetector();

        var leadingSamples = SampleRate * 2; // 2 seconds of silence
        var contentSamples = SampleRate; // 1 second of content

        var audio = CreateAudioWithContent(
            leadingSilenceSamples: leadingSamples,
            contentSamples: contentSamples,
            trailingSilenceSamples: 0);

        var result = detector.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.NotNull(result);

        // Start sample should be ~88200 (2 seconds * 44100), NOT 0
        Assert.InRange(result!.StartSample, leadingSamples - 2048, leadingSamples + 2048);

        // This is crucial: if we were to process only samples[StartSample..EndSample],
        // and an onset detector returns time 0.5s, the ACTUAL time in the recording
        // would be: LeadingSilence + 0.5s = 2.0s + 0.5s = 2.5s from recording start

        // Verify we can calculate absolute time correctly
        var onsetRelativeToSlice = 0.5; // seconds into the content
        var absoluteTime = result.LeadingSilence.TotalSeconds + onsetRelativeToSlice;
        Assert.InRange(absoluteTime, 2.4, 2.6); // ~2.5 seconds from recording start
    }

    [Fact]
    public void DetectBoundaries_SensitiveThreshold_DetectsQuieterContent()
    {
        // More sensitive threshold (-60dB instead of -40dB)
        var sensitiveDetector = new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { ThresholdDb = -60.0f });
        var normalDetector = new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { ThresholdDb = -40.0f });

        // Create very quiet tone
        // -60dB threshold = 10^(-60/20) = 0.001 linear amplitude
        // For sine wave, RMS = amplitude / sqrt(2), so we need amplitude = 0.001 * sqrt(2) ≈ 0.00141
        var audio = CreateTone(SampleRate, 440.0f, amplitude: 0.002f); // Very quiet but above -60dB

        var sensitiveResult = sensitiveDetector.DetectBoundaries(PipelineProgress.Null, audio);
        var normalResult = normalDetector.DetectBoundaries(PipelineProgress.Null, audio);

        // Sensitive detector should find content
        Assert.NotNull(sensitiveResult);

        // Normal detector should not (amplitude is below -40dB threshold)
        Assert.Null(normalResult);
    }

    [Fact]
    public void DetectBoundaries_StereoAudio_HandlesCorrectly()
    {
        var detector = new EnergyBasedBoundaryDetector();

        // Create stereo audio with content in both channels
        var leftChannel = CreateTone(SampleRate, 440.0f);
        var rightChannel = CreateTone(SampleRate, 880.0f);

        var stereoSamples = new float[SampleRate * 2]; // Interleaved stereo
        for (int i = 0; i < SampleRate; i++)
        {
            stereoSamples[i * 2] = leftChannel.Samples.Span[i];
            stereoSamples[i * 2 + 1] = rightChannel.Samples.Span[i];
        }

        var stereoAudio = new AudioBuffer(stereoSamples, SampleRate, 2);

        var result = detector.DetectBoundaries(PipelineProgress.Null, stereoAudio);

        Assert.NotNull(result);
        // Should detect content (converted to mono internally)
        Assert.Equal(0, result!.StartSample);
    }

    [Fact]
    public void DetectBoundaries_SmallWindowSize_MorePreciseBoundaries()
    {
        var smallWindow = new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { WindowSize = 512 });
        var largeWindow = new EnergyBasedBoundaryDetector(new BoundaryDetectionOptions { WindowSize = 4096 });

        var leadingSamples = SampleRate / 2; // 0.5 seconds
        var audio = CreateAudioWithContent(
            leadingSilenceSamples: leadingSamples,
            contentSamples: SampleRate,
            trailingSilenceSamples: 0);

        var smallResult = smallWindow.DetectBoundaries(PipelineProgress.Null, audio);
        var largeResult = largeWindow.DetectBoundaries(PipelineProgress.Null, audio);

        Assert.NotNull(smallResult);
        Assert.NotNull(largeResult);

        // Smaller window should be closer to actual boundary
        var smallError = Math.Abs(smallResult!.StartSample - leadingSamples);
        var largeError = Math.Abs(largeResult!.StartSample - leadingSamples);

        Assert.True(smallError <= largeError);
    }

    /// <summary>
    /// Creates an audio buffer with silence.
    /// </summary>
    private static AudioBuffer CreateSilence(int samples)
    {
        var buffer = new float[samples];
        return new AudioBuffer(buffer, SampleRate, 1);
    }

    /// <summary>
    /// Creates an audio buffer with specified silence and content pattern.
    /// </summary>
    private static AudioBuffer CreateAudioWithContent(
        int leadingSilenceSamples,
        int contentSamples,
        int trailingSilenceSamples)
    {
        var totalSamples = leadingSilenceSamples + contentSamples + trailingSilenceSamples;
        var buffer = new float[totalSamples];

        // Fill content section with tone
        var tone = CreateTone(contentSamples, 440.0f);
        tone.Samples.Span.CopyTo(buffer.AsSpan(leadingSilenceSamples, contentSamples));

        return new AudioBuffer(buffer, SampleRate, 1);
    }

    /// <summary>
    /// Creates a simple sine wave tone.
    /// </summary>
    private static AudioBuffer CreateTone(int samples, float frequency, float amplitude = 0.5f)
    {
        var buffer = new float[samples];
        var phaseIncrement = 2.0 * Math.PI * frequency / SampleRate;

        for (int i = 0; i < samples; i++)
        {
            buffer[i] = amplitude * (float)Math.Sin(i * phaseIncrement);
        }

        return new AudioBuffer(buffer, SampleRate, 1);
    }
}
