using System.Numerics.Tensors;

namespace StaffSharp.Audio.Numerics;

/// <summary>
/// High-performance audio processing operations using TensorPrimitives.
/// Automatically leverages SIMD acceleration when available.
/// </summary>
public static class SimdOps
{
    /// <summary>
    /// Computes RMS (Root Mean Square) energy of an audio buffer.
    /// </summary>
    public static float ComputeRms(ReadOnlySpan<float> buffer)
    {
        if (buffer.Length == 0)
            return 0;

        var sumOfSquares = TensorPrimitives.SumOfSquares(buffer);
        return MathF.Sqrt(sumOfSquares / buffer.Length);
    }

    /// <summary>
    /// Normalizes buffer to [-1.0, 1.0] range.
    /// Modifies buffer in-place.
    /// </summary>
    public static void Normalize(Span<float> buffer)
    {
        if (buffer.Length == 0)
            return;

        var maxAbs = TensorPrimitives.MaxMagnitude(buffer);

        if (maxAbs < 1e-10f) // Avoid divide by zero
            return;

        TensorPrimitives.Multiply(buffer, 1.0f / maxAbs, buffer);
    }

    /// <summary>
    /// Applies a window function to the buffer element-wise.
    /// Modifies buffer in-place.
    /// </summary>
    public static void ApplyWindow(Span<float> buffer, ReadOnlySpan<float> window)
    {
        if (buffer.Length != window.Length)
            throw new ArgumentException("Buffer and window must have same length");

        TensorPrimitives.Multiply(buffer, window, buffer);
    }

    /// <summary>
    /// Computes autocorrelation at a specific lag.
    /// Used for pitch detection.
    /// </summary>
    public static float ComputeAutocorrelation(ReadOnlySpan<float> buffer, int lag)
    {
        if (lag < 0 || lag >= buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(lag));

        var length = buffer.Length - lag;
        return TensorPrimitives.Dot(buffer[..length], buffer.Slice(lag, length));
    }
}
