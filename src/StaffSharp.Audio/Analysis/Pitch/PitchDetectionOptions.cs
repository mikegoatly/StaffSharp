using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Audio.Analysis.Pitch;

/// <summary>
/// Options for pitch detection algorithms implementing <see cref="IPitchDetector"/>.
/// </summary>
public record PitchDetectionOptions : DiagnosticsOptions
{
    /// <summary>
    /// Gets or initializes the minimum detectable frequency in Hz.
    /// Default: 80.0 Hz (approximately E2).
    /// </summary>
    public double MinFrequency { get; init; } = 80.0;

    /// <summary>
    /// Gets or initializes the maximum detectable frequency in Hz.
    /// Default: 1000.0 Hz (approximately B5).
    /// </summary>
    public double MaxFrequency { get; init; } = 1000.0;

    /// <summary>
    /// Gets or initializes the detection threshold.
    /// Default: 0.15 (YIN threshold).
    /// </summary>
    public float Threshold { get; init; } = 0.15f;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when frequency range is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is out of valid range.</exception>
    public void Validate()
    {
        if (MinFrequency <= 0 || MaxFrequency <= MinFrequency)
            throw new ArgumentException("Invalid frequency range: MinFrequency must be positive and less than MaxFrequency");
        if (Threshold <= 0 || Threshold >= 1)
            throw new ArgumentOutOfRangeException(nameof(Threshold), "Threshold must be in (0, 1)");
    }
}
