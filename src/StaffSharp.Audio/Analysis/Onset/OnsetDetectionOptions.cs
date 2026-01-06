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
    /// Must be positive. Default: 2.0.
    /// </summary>
    public float Threshold { get; init; } = 2.0f;

    /// <summary>
    /// Gets or initializes the minimum time interval between consecutive onsets in seconds.
    /// Must be non-negative. Default: 0.40 seconds (400ms).
    /// </summary>
    public float MinOnsetIntervalSeconds { get; init; } = 0.40f;

    /// <summary>
    /// Gets or initializes whether to apply logarithmic compression (log(1+x)).
    /// Crucial for detecting onsets in strictly harmonic instruments or dense mixtures.
    /// Default: true.
    /// </summary>
    public bool ApplyLogarithmicCompression { get; init; } = true;

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
