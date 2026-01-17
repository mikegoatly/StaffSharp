namespace StaffSharp.Audio.Analysis.Boundaries;

/// <summary>
/// Options for audio boundary detection algorithms implementing <see cref="IAudioBoundaryDetector"/>.
/// </summary>
public record BoundaryDetectionOptions
{
    /// <summary>
    /// Gets or initializes the energy threshold in dB.
    /// Levels below this are considered silence.
    /// Must be negative. Default: -40.0 dB.
    /// </summary>
    public float ThresholdDb { get; init; } = -40.0f;

    /// <summary>
    /// Gets or initializes the window size in samples for energy calculation.
    /// Default: 2048 (~46ms at 44.1kHz).
    /// </summary>
    public int WindowSize { get; init; } = 2048;

    /// <summary>
    /// Gets or initializes the minimum number of samples for valid content.
    /// Default: 4410 (~100ms at 44.1kHz).
    /// </summary>
    public int MinContentSamples { get; init; } = 4410;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public void Validate()
    {
        if (ThresholdDb >= 0)
        {
            throw new ArgumentException("Threshold must be negative (dB)", nameof(ThresholdDb));
        }

        if (WindowSize <= 0)
        {
            throw new ArgumentException("Window size must be positive", nameof(WindowSize));
        }

        if (MinContentSamples <= 0)
        {
            throw new ArgumentException("Minimum content samples must be positive", nameof(MinContentSamples));
        }
    }
}
