namespace StaffSharp.MachineLearning.Options;

/// <summary>
/// Configuration options for harmonic suppression in polyphonic transcription.
/// </summary>
public sealed record HarmonicSuppressionOptions(bool SuppressHarmonics = true)
{
    /// <summary>
    /// Maximum time difference between note onsets to consider them as potential harmonics.
    /// Notes starting within this window are candidates for harmonic filtering.
    /// Default: 50ms
    /// </summary>
    public TimeSpan TemporalWindow { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Velocity ratio threshold for harmonic suppression.
    /// Try to keep this small to only suppress clear overtones, otherwise you may lose softer 
    /// simultaneous notes.
    /// </summary>
    public float VelocityRatio { get; init; } = 0.4f;
}
