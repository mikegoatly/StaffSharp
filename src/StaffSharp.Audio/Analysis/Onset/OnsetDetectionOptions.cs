using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Audio.Analysis.Onset;

/// <summary>
/// Options for onset detection algorithms implementing <see cref="IOnsetDetector"/>.
/// </summary>
public record OnsetDetectionOptions : DiagnosticsOptions
{
    /// <summary>
    /// Gets or initializes the FFT frame size in samples.
    /// Must be a power of 2. Default: 2048.
    /// </summary>
    public int FrameSize { get; init; } = 2048;

    /// <summary>
    /// Gets or initializes the hop size in samples (must be ≤ frame size).
    /// Default: 512.
    /// </summary>
    public int HopSize { get; init; } = 512;

    /// <summary>
    /// Gets or initializes the onset detection threshold.
    /// Must be positive. Default: 0.3.
    /// </summary>
    public float Threshold { get; init; } = 0.3f;

    /// <summary>
    /// Gets or initializes the minimum time interval between consecutive onsets in seconds.
    /// Must be non-negative. Default: 0.05 seconds (50ms).
    /// </summary>
    public float MinOnsetIntervalSeconds { get; init; } = 0.05f;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when frame size is not a power of 2.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range.</exception>
    public void Validate()
    {
        if (FrameSize <= 0 || (FrameSize & (FrameSize - 1)) != 0)
            throw new ArgumentException("Frame size must be a power of 2", nameof(FrameSize));
        if (HopSize <= 0 || HopSize > FrameSize)
            throw new ArgumentOutOfRangeException(nameof(HopSize), "Hop size must be positive and <= frame size");
        if (Threshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(Threshold), "Threshold must be positive");
        if (MinOnsetIntervalSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(MinOnsetIntervalSeconds), "Minimum interval must be non-negative");
    }
}
