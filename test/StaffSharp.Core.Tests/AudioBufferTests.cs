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
        var (normalized, _) = buffer.Normalize(1.0f);

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
        var (normalized, stats) = buffer.Normalize(1.0f);

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
        var samples = new float[] { 1.0f, 0.5f };
        var buffer = new AudioBuffer(samples, 44100, 1);

        // Act
        var (normalized, _) = buffer.Normalize();

        // Assert
        Assert.Same(buffer, normalized);
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
}
