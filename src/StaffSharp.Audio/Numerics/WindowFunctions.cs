namespace StaffSharp.Audio.Numerics;

/// <summary>
/// Standard window functions for DSP operations.
/// Used to reduce spectral leakage in FFT-based algorithms.
/// </summary>
public static class WindowFunctions
{
    /// <summary>
    /// Creates a Hann (Hanning) window.
    /// Smooth taper, good frequency resolution, commonly used for general-purpose FFT.
    /// </summary>
    public static float[] CreateHannWindow(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Window size must be positive");

        var window = new float[size];
        for (int i = 0; i < size; i++)
        {
            window[i] = 0.5f * (1.0f - MathF.Cos(2.0f * MathF.PI * i / (size - 1)));
        }
        return window;
    }

    /// <summary>
    /// Creates a Hamming window.
    /// Similar to Hann but with smaller side lobes, better for spectral analysis.
    /// </summary>
    public static float[] CreateHammingWindow(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Window size must be positive");

        var window = new float[size];
        for (int i = 0; i < size; i++)
        {
            window[i] = 0.54f - 0.46f * MathF.Cos(2.0f * MathF.PI * i / (size - 1));
        }
        return window;
    }

    /// <summary>
    /// Creates a Blackman window.
    /// Excellent side lobe suppression, used when spectral leakage is critical.
    /// </summary>
    public static float[] CreateBlackmanWindow(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Window size must be positive");

        var window = new float[size];
        for (int i = 0; i < size; i++)
        {
            var alpha = 2.0f * MathF.PI * i / (size - 1);
            window[i] = 0.42f - 0.5f * MathF.Cos(alpha) + 0.08f * MathF.Cos(2.0f * alpha);
        }
        return window;
    }

    /// <summary>
    /// Creates a rectangular (boxcar) window.
    /// No tapering - mainly used for comparison or when no windowing is desired.
    /// </summary>
    public static float[] CreateRectangularWindow(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Window size must be positive");

        var window = new float[size];
        Array.Fill(window, 1.0f);
        return window;
    }
}
