namespace StaffSharp.Audio.Analysis.Pitch;

/// <summary>
/// Result from pitch detection, including frequency and confidence.
/// </summary>
public readonly record struct PitchDetectionResult
{
    public PitchDetectionResult(double frequencyHz, float confidence)
    {
        FrequencyHz = frequencyHz;
        Confidence = confidence;
    }

    /// <summary>
    /// Detected pitch in Hz. 0 if no pitch detected.
    /// </summary>
    public double FrequencyHz { get; }

    /// <summary>
    /// Confidence score [0.0, 1.0]. Higher is more confident.
    /// </summary>
    public float Confidence { get; }

    /// <summary>
    /// Whether a pitch was successfully detected.
    /// </summary>
    public bool IsPitched => FrequencyHz > 0 && Confidence > 0;
}
