using System;
using Xunit;
using StaffSharp.Audio;

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
}
