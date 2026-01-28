using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests;

public class AudioBufferTests
{
    [Fact]
    public void Normalize_WithPositivePeak_ScalesCorrectly()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.5f, 0.2f }; // Max is 0.5
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, _) = buffer.Normalize(1.0f, 0.0f, 0.0f);

        // Assert
        // Gain should be 1.0 / 0.5 = 2.0
        // Expected: 0.2, 1.0, 0.4
        var span = normalized.Samples.Span;
        Assert.Equal(0.2f, span[0], 4);
        Assert.Equal(1.0f, span[1], 4);
        Assert.Equal(0.4f, span[2], 4);
    }

    [Fact]
    public void Normalize_WithNegativePeak_ScalesCorrectly()
    {
        // Arrange
        // Negative peak has larger magnitude than positive values
        var samples = new float[] { 0.1f, -0.5f, 0.2f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, stats) = buffer.Normalize(1.0f, 0.0f, 0.0f);

        // Assert
        // Max magnitude is |-0.5| = 0.5.
        // Gain should be 1.0 / 0.5 = 2.0.
        // Expected: 0.2, -1.0, 0.4
        var span = normalized.Samples.Span;
        Assert.Equal(1.0f, Math.Abs(span[1]), 4); // Peak should be at target
        Assert.Equal(0.2f, span[0], 4);
        Assert.Equal(-1.0f, span[1], 4);
        Assert.Equal(0.4f, span[2], 4);

        // Stats
        Assert.Equal(0.5f, Math.Abs(stats.OriginalPeakAmplitude), 4);
        Assert.Equal(2.0f, stats.GainApplied, 4);
    }

    [Fact]
    public void Normalize_Silence_DoesNothing()
    {
        // Arrange
        var samples = new float[] { 0.0f, 0.0f, 0.0f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, _) = buffer.Normalize();

        // Assert
        Assert.Same(buffer, normalized); // Should return same instance
    }

    [Fact]
    public void Normalize_NearSilence_DoesNothing()
    {
        // Arrange
        var samples = new float[] { 1e-7f, -1e-7f }; // Below threshold
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, _) = buffer.Normalize();

        // Assert
        Assert.Same(buffer, normalized);
    }

    [Fact]
    public void Normalize_AlreadyNormalized_DoesNothing()
    {
        // Arrange
        var samples = new float[] { 0.6f, 0.5f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, _) = buffer.Normalize();

        // Assert
        Assert.Same(buffer, normalized);
    }

    [Fact]
    public void Normalize_WithinAllowedRange_DoesNothing()
    {
        // Arrange
        var samples = new float[] { 0.5f, 0.4f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, _) = buffer.Normalize(0.6f, 0.3f, 0.8f);

        // Assert
        Assert.Same(buffer, normalized);
    }

    [Fact]
    public void Normalize_OutsideAllowedRange_Normalizes()
    {
        // Arrange
        var samples = new float[] { 0.9f, 0.3f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, _) = buffer.Normalize(0.6f, 0.4f, 0.8f);

        // Assert
        // Peak 0.9 > 0.8, so normalize to 0.6
        var span = normalized.Samples.Span;
        Assert.Equal(0.6f, span[0], 4);
        Assert.Equal(0.2f, span[1], 4); // 0.3 * (0.6 / 0.9)
    }

    [Fact]
    public void ToMono_StereoToMono_AveragesChannels()
    {
        // Arrange
        // L, R, L, R
        var samples = new float[] { 1.0f, 0.0f, 0.5f, 0.5f };
        var buffer = new AudioBuffer(samples, 44100, 2);

        // Act
        var mono = buffer.ToMono();

        // Assert
        Assert.Equal(1, mono.Channels);
        Assert.Equal(2, mono.SampleCount);

        var span = mono.Samples.Span;
        Assert.Equal(0.5f, span[0]); // (1+0)/2
        Assert.Equal(0.5f, span[1]); // (0.5+0.5)/2
    }

    [Fact]
    public void Resample_SameRate_ReturnsSameBuffer()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f, 0.3f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var resampled = buffer.Resample(44100);

        // Assert
        Assert.Same(buffer, resampled);
    }

    [Fact]
    public void Resample_Upsampling_InterpolatesCorrectly()
    {
        // Arrange
        // Simple case: double the sample rate
        var samples = new float[] { 0.0f, 1.0f, 0.0f };
        var buffer = new AudioBuffer(samples, 1000, 1);

        // Act
        var resampled = buffer.Resample(2000);

        // Assert
        Assert.Equal(2000, resampled.SampleRate);
        Assert.Equal(6, resampled.SampleCount); // 3 * 2 = 6

        var span = resampled.Samples.Span;

        // Should have interpolated values between original samples
        Assert.Equal(0.0f, span[0], 4);
        Assert.Equal(0.5f, span[1], 4); // Halfway between 0.0 and 1.0
        Assert.Equal(1.0f, span[2], 4);
        Assert.Equal(0.5f, span[3], 4); // Halfway between 1.0 and 0.0
        Assert.Equal(0.0f, span[4], 4);
    }

    [Fact]
    public void Resample_Downsampling_ReducesSampleCount()
    {
        // Arrange
        var samples = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
        var buffer = new AudioBuffer(samples, 5000, 1);

        // Act
        var resampled = buffer.Resample(2500); // Half the rate

        // Assert
        Assert.Equal(2500, resampled.SampleRate);
        Assert.Equal(2, resampled.SampleCount); // 5 * 0.5 = 2.5, truncated to 2

        var span = resampled.Samples.Span;

        // First sample should be at index 0
        Assert.Equal(0.0f, span[0], 4);
        // Second sample should be interpolated from around index 2
        Assert.InRange(span[1], 0.4f, 0.6f);
    }

    [Fact]
    public void Resample_PreservesSampleRate()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var buffer = new AudioBuffer(samples, 22050, 1);

        // Act
        var resampled = buffer.Resample(44100);

        // Assert
        Assert.Equal(44100, resampled.SampleRate);
        Assert.Equal(1, resampled.Channels);
    }

    [Fact]
    public void Resample_PreservesChannelCount()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var buffer = new AudioBuffer(samples, 44100, 2);

        // Act
        var resampled = buffer.Resample(48000);

        // Assert
        Assert.Equal(2, resampled.Channels);
    }

    [Fact]
    public void Resample_InvalidSampleRate_ThrowsException()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Resample(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Resample(-1000));
    }

    [Fact]
    public void Resample_SingleSample_HandlesEdgeCase()
    {
        // Arrange
        var samples = new float[] { 0.5f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var resampled = buffer.Resample(22050);

        // Assert
        Assert.Equal(22050, resampled.SampleRate);
        Assert.True(resampled.SampleCount >= 0);
    }

    [Fact]
    public void DetectContent_AllSilence_ReturnsFullDuration()
    {
        // Arrange
        var samples = AudioSignalBuilder.Silence(duration: 0.1, sampleRate: 1000);
        var buffer = new AudioBuffer(samples, 1000, 1);

        // Act
        var (start, end) = buffer.DetectContent();

        // Assert
        Assert.Equal(TimeSpan.Zero, start);
        Assert.Equal(TimeSpan.FromSeconds(buffer.DurationSeconds), end);
    }

    [Fact]
    public void DetectContent_LeadingSilence_DetectsCorrectStart()
    {
        // Arrange - 1s silence + 4s signal = 5s total
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(1000)
            .WithDuration(5.0)
            .AtTime(1.0).AddSine(100, amplitude: 0.1, durationSeconds: 4.0)
            .Build();
        var buffer = new AudioBuffer(samples, 1000, 1);

        // Act - use smaller frame size for precise detection
        var (start, end) = buffer.DetectContent(frameSize: 512, hopSize: 256);

        // Assert
        // Content should start after some silence (not at zero)
        Assert.True(start.TotalSeconds > 0.5, $"Expected start > 0.5s, got {start.TotalSeconds}s");
        // Content should end at or near the end
        Assert.True(end.TotalSeconds >= 4.5, $"Expected end >= 4.5s, got {end.TotalSeconds}s");
    }

    [Fact]
    public void DetectContent_TrailingSilence_DetectsCorrectEnd()
    {
        // Arrange - 4s signal + 1s silence = 5s total
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(1000)
            .WithDuration(5.0)
            .AtTime(0.0).AddSine(100, amplitude: 0.1, durationSeconds: 4.0)
            .Build();
        var buffer = new AudioBuffer(samples, 1000, 1);

        // Act - use smaller frame size for precise detection
        var (start, end) = buffer.DetectContent(frameSize: 512, hopSize: 256);

        // Assert
        Assert.Equal(TimeSpan.Zero, start);
        // Content should end before the full duration (trailing silence detected)
        Assert.True(end.TotalSeconds < 4.5, $"Expected end < 4.5s, got {end.TotalSeconds}s");
    }

    [Fact]
    public void DetectContent_LeadingAndTrailingSilence_DetectsBoth()
    {
        // Arrange - 1s silence + 3s signal + 1s silence = 5s total
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(1000)
            .WithDuration(5.0)
            .AtTime(1.0).AddSine(100, amplitude: 0.1, durationSeconds: 3.0)
            .Build();
        var buffer = new AudioBuffer(samples, 1000, 1);

        // Act - use smaller frame size for precise detection
        var (start, end) = buffer.DetectContent(frameSize: 512, hopSize: 256);

        // Assert
        // Content should start after leading silence
        Assert.True(start.TotalSeconds > 0.5, $"Expected start > 0.5s, got {start.TotalSeconds}s");
        // Content should end before trailing silence
        Assert.True(end.TotalSeconds < 4.5, $"Expected end < 4.5s, got {end.TotalSeconds}s");
        // Content duration should be less than full duration
        Assert.True((end - start).TotalSeconds < 4.0, $"Expected content duration < 4s, got {(end - start).TotalSeconds}s");
    }

    [Fact]
    public void DetectContent_NoSilence_ReturnsFullDuration()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(frequency: 100, duration: 0.1, sampleRate: 1000, amplitude: 0.1);
        var buffer = new AudioBuffer(samples, 1000, 1);

        // Act
        var (start, end) = buffer.DetectContent();

        // Assert
        Assert.Equal(TimeSpan.Zero, start);
        Assert.Equal(TimeSpan.FromSeconds(buffer.DurationSeconds), end);
    }

    [Fact]
    public void DetectContent_StereoInput_HandlesCorrectly()
    {
        // Arrange - stereo with L=silence, R=signal
        var samples = new float[200]; // 100 frames stereo
        for (int i = 0; i < 100; i++)
        {
            samples[i * 2] = 0.0f;     // Left channel: silence
            samples[i * 2 + 1] = 0.1f; // Right channel: signal
        }
        var buffer = new AudioBuffer(samples, 1000, 2);

        // Act
        var (start, end) = buffer.DetectContent();

        // Assert - should detect content (mixed down to mono with signal)
        Assert.Equal(TimeSpan.Zero, start);
        Assert.True(end > TimeSpan.Zero);
    }

    [Fact]
    public void NormalizeRms_QuietAudio_IncreasesGain()
    {
        // Arrange
        // Create audio with RMS of 0.01 (very quiet)
        var samples = new float[] { 0.01f, -0.01f, 0.01f, -0.01f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, stats) = buffer.NormalizeRms(targetRms: 0.1f);

        // Assert
        // Gain should be ~10x to get from 0.01 RMS to 0.1 RMS
        Assert.True(stats.GainApplied > 5.0f);
        Assert.Equal(0.01f, stats.OriginalRms, 2);

        // Verify samples are louder
        var span = normalized.Samples.Span;
        Assert.True(Math.Abs(span[0]) > 0.05f);
    }

    [Fact]
    public void NormalizeRms_LoudAudio_DecreasesGain()
    {
        // Arrange
        // Create audio with RMS of 0.5 (loud)
        var samples = new float[] { 0.5f, -0.5f, 0.5f, -0.5f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, stats) = buffer.NormalizeRms(targetRms: 0.1f);

        // Assert
        // Gain should be 0.2x to get from 0.5 RMS to 0.1 RMS
        Assert.True(stats.GainApplied < 0.5f);
        Assert.Equal(0.5f, stats.OriginalRms, 2);

        // Verify samples are quieter
        var span = normalized.Samples.Span;
        Assert.True(Math.Abs(span[0]) < 0.2f);
    }

    [Fact]
    public void NormalizeRms_Silence_ReturnsOriginal()
    {
        // Arrange
        var samples = AudioSignalBuilder.Silence(duration: 0.1, sampleRate: 44100);
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, stats) = buffer.NormalizeRms();

        // Assert
        Assert.Equal(0.0f, stats.OriginalRms);
        Assert.Equal(1.0f, stats.GainApplied); // No gain applied
        Assert.Same(buffer, normalized);
    }

    [Fact]
    public void NormalizeRms_FullBuffer_CalculatesCorrectRms()
    {
        // Arrange
        // Create signal with known RMS
        // For a square wave alternating between +A and -A, RMS = A
        var samples = new float[] { 0.3f, -0.3f, 0.3f, -0.3f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, stats) = buffer.NormalizeRms(targetRms: 0.1f);

        // Assert
        // Original RMS should be 0.3
        Assert.Equal(0.3f, stats.OriginalRms, 2);
        // Gain should be 0.1 / 0.3 ≈ 0.333
        Assert.Equal(0.1f / 0.3f, stats.GainApplied, 2);
    }

    [Fact]
    public void NormalizeRms_PreservesWaveformShape()
    {
        // Arrange
        var samples = new float[] { 0.1f, 0.2f, 0.3f, 0.2f, 0.1f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, stats) = buffer.NormalizeRms(targetRms: 0.5f);

        // Assert - relative amplitudes should be preserved
        var span = normalized.Samples.Span;
        var gain = stats.GainApplied;

        Assert.Equal(0.1f * gain, span[0], 4);
        Assert.Equal(0.2f * gain, span[1], 4);
        Assert.Equal(0.3f * gain, span[2], 4);
        Assert.Equal(0.2f * gain, span[3], 4);
        Assert.Equal(0.1f * gain, span[4], 4);
    }
}
