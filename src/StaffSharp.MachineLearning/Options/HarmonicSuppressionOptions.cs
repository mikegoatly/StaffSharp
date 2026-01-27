namespace StaffSharp.MachineLearning.Options;

/// <summary>
/// Configuration options for harmonic suppression in polyphonic transcription.
/// </summary>
public sealed record HarmonicSuppressionOptions(bool SuppressHarmonics = true)
{
    /// <summary>
    /// Maximum time difference (in milliseconds) between note onsets to consider them as potential harmonics.
    /// Notes starting within this window are candidates for harmonic filtering.
    /// Default: 50ms
    /// </summary>
    public double TemporalWindowMs { get; init; } = 50.0;

    /// <summary>
    /// Velocity ratio threshold for harmonic suppression.
    /// A harmonic must be quieter than (fundamental velocity * ratio) to be removed.
    /// For example, 0.9 means the harmonic must be &lt; 90% of the fundamental's volume.
    /// Higher values = more aggressive suppression. Set to 1.0 to remove all harmonics regardless of volume.
    /// Default: 0.9
    /// </summary>
    public float VelocityRatio { get; init; } = 0.9f;
}
