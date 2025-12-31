using StaffSharp.Audio.Numerics;

namespace StaffSharp.Audio.Tests.Numerics;

public class SimdOpsTests
{
    [Fact]
    public void ComputeRms_EmptyBuffer_ReturnsZero()
    {
        var buffer = ReadOnlySpan<float>.Empty;
        var rms = SimdOps.ComputeRms(buffer);
        Assert.Equal(0f, rms);
    }

    [Fact]
    public void ComputeRms_ConstantBuffer_ReturnsAbsoluteValue()
    {
        var buffer = new float[] { 2.0f, 2.0f, 2.0f, 2.0f };
        var rms = SimdOps.ComputeRms(buffer);
        Assert.Equal(2.0f, rms, precision: 5);
    }

    [Fact]
    public void ComputeRms_MixedValues_ComputesCorrectly()
    {
        // RMS of [1, 2, 3, 4] = sqrt((1 + 4 + 9 + 16) / 4) = sqrt(7.5) ≈ 2.739
        var buffer = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
        var rms = SimdOps.ComputeRms(buffer);
        var expected = MathF.Sqrt(7.5f);
        Assert.Equal(expected, rms, precision: 5);
    }

    [Fact]
    public void ComputeRms_NegativeValues_TreatsAsSquares()
    {
        // RMS of [-2, 2, -2, 2] = sqrt(16 / 4) = 2
        var buffer = new float[] { -2.0f, 2.0f, -2.0f, 2.0f };
        var rms = SimdOps.ComputeRms(buffer);
        Assert.Equal(2.0f, rms, precision: 5);
    }

    [Fact]
    public void ComputeRms_LargeBuffer_HandlesCorrectly()
    {
        // Test with buffer larger than typical SIMD vector size
        var buffer = new float[1000];
        Array.Fill(buffer, 1.0f);
        var rms = SimdOps.ComputeRms(buffer);
        Assert.Equal(1.0f, rms, precision: 5);
    }

    [Fact]
    public void Normalize_EmptyBuffer_DoesNothing()
    {
        var buffer = Array.Empty<float>();
        SimdOps.Normalize(buffer);
        Assert.Empty(buffer);
    }

    [Fact]
    public void Normalize_ZeroBuffer_DoesNothing()
    {
        var buffer = new float[] { 0f, 0f, 0f };
        SimdOps.Normalize(buffer);
        Assert.All(buffer, val => Assert.Equal(0f, val));
    }

    [Fact]
    public void Normalize_PositiveValues_NormalizesToOne()
    {
        var buffer = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
        SimdOps.Normalize(buffer);

        Assert.Equal(0.25f, buffer[0], precision: 5);
        Assert.Equal(0.5f, buffer[1], precision: 5);
        Assert.Equal(0.75f, buffer[2], precision: 5);
        Assert.Equal(1.0f, buffer[3], precision: 5);
    }

    [Fact]
    public void Normalize_NegativeValues_HandlesCorrectly()
    {
        var buffer = new float[] { -4.0f, -2.0f, 2.0f, 4.0f };
        SimdOps.Normalize(buffer);

        // Max magnitude is 4, so all values divided by 4
        Assert.Equal(-1.0f, buffer[0], precision: 5);
        Assert.Equal(-0.5f, buffer[1], precision: 5);
        Assert.Equal(0.5f, buffer[2], precision: 5);
        Assert.Equal(1.0f, buffer[3], precision: 5);
    }

    [Fact]
    public void Normalize_LargeBuffer_NormalizesCorrectly()
    {
        var buffer = new float[1000];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }

        SimdOps.Normalize(buffer);

        Assert.Equal(0f, buffer[0], precision: 5);
        Assert.Equal(1f, buffer[999], precision: 5);
        Assert.All(buffer, val => Assert.InRange(val, 0f, 1f));
    }

    [Fact]
    public void ApplyWindow_MismatchedLengths_ThrowsException()
    {
        var buffer = new float[10];
        var window = new float[5];

        Assert.Throws<ArgumentException>(() => SimdOps.ApplyWindow(buffer, window));
    }

    [Fact]
    public void ApplyWindow_ValidInputs_MultipliesElementWise()
    {
        var buffer = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
        var window = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };

        SimdOps.ApplyWindow(buffer, window);

        Assert.Equal(0.5f, buffer[0], precision: 5);
        Assert.Equal(1.0f, buffer[1], precision: 5);
        Assert.Equal(1.5f, buffer[2], precision: 5);
        Assert.Equal(2.0f, buffer[3], precision: 5);
    }

    [Fact]
    public void ApplyWindow_LargeBuffer_AppliesCorrectly()
    {
        var buffer = new float[1000];
        var window = new float[1000];
        Array.Fill(buffer, 2.0f);
        Array.Fill(window, 0.5f);

        SimdOps.ApplyWindow(buffer, window);

        Assert.All(buffer, val => Assert.Equal(1.0f, val, precision: 5));
    }

    [Fact]
    public void ComputeAutocorrelation_NegativeLag_ThrowsException()
    {
        var buffer = new float[10];
        Assert.Throws<ArgumentOutOfRangeException>(() => SimdOps.ComputeAutocorrelation(buffer, -1));
    }

    [Fact]
    public void ComputeAutocorrelation_LagTooLarge_ThrowsException()
    {
        var buffer = new float[10];
        Assert.Throws<ArgumentOutOfRangeException>(() => SimdOps.ComputeAutocorrelation(buffer, 10));
    }

    [Fact]
    public void ComputeAutocorrelation_ZeroLag_ReturnsSumOfSquares()
    {
        var buffer = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
        var result = SimdOps.ComputeAutocorrelation(buffer, 0);

        // Sum of squares: 1 + 4 + 9 + 16 = 30
        Assert.Equal(30.0f, result, precision: 5);
    }

    [Fact]
    public void ComputeAutocorrelation_NonZeroLag_ComputesCorrectly()
    {
        var buffer = new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
        var result = SimdOps.ComputeAutocorrelation(buffer, 1);

        // Dot product of [1, 2, 3, 4] and [2, 3, 4, 5]
        // = 1*2 + 2*3 + 3*4 + 4*5 = 2 + 6 + 12 + 20 = 40
        Assert.Equal(40.0f, result, precision: 5);
    }

    [Fact]
    public void ComputeAutocorrelation_PeriodicSignal_ShowsPeaksAtPeriod()
    {
        // Create a simple periodic signal: [1, 0, 1, 0, 1, 0, 1, 0]
        var buffer = new float[] { 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f };

        var lag0 = SimdOps.ComputeAutocorrelation(buffer, 0); // Should be max (sum of squares)
        var lag1 = SimdOps.ComputeAutocorrelation(buffer, 1); // Should be 0 (opposite phase)
        var lag2 = SimdOps.ComputeAutocorrelation(buffer, 2); // Should be high (same phase)

        Assert.Equal(4.0f, lag0, precision: 5); // Sum of squares = 4
        Assert.Equal(0.0f, lag1, precision: 5); // Opposite phase
        Assert.Equal(3.0f, lag2, precision: 5); // Same phase (3 pairs aligned)
    }

    [Fact]
    public void ComputeAutocorrelation_LargeBuffer_ComputesEfficiently()
    {
        var buffer = new float[10000];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = MathF.Sin(2 * MathF.PI * i / 100); // 100-sample period sine wave
        }

        // Should complete without throwing and return reasonable value
        var result = SimdOps.ComputeAutocorrelation(buffer, 100);
        Assert.True(result > 0); // Same phase should give positive correlation
    }
}
