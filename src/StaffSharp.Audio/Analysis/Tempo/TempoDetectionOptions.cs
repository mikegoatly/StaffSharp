using StaffSharp.Notation;

namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// Options for tempo detection algorithms implementing <see cref="ITempoDetector"/>.
/// </summary>
public record TempoDetectionOptions
{
    /// <summary>
    /// Gets or initializes the tempo detection algorithm to use.
    /// Default: <see cref="TempoDetectorType.CombFilter"/>.
    /// </summary>
    public TempoDetectorType DetectorType { get; init; } = TempoDetectorType.CombFilter;

    /// <summary>
    /// Gets or initializes the minimum detectable tempo in BPM.
    /// Default: 40.0 BPM.
    /// </summary>
    public double MinBpm { get; init; } = 40.0;

    /// <summary>
    /// Gets or initializes the maximum detectable tempo in BPM.
    /// Default: 320.0 BPM.
    /// </summary>
    public double MaxBpm { get; init; } = 320.0;

    /// <summary>
    /// Gets or initializes the default time signature for the tempo map.
    /// If null, 4/4 (common time) is used. Default: null.
    /// </summary>
    public TimeSignature? DefaultTimeSignature { get; init; }

    // ========================================================================
    // Comb Filter Tempo Detector Options
    // ========================================================================

    /// <summary>
    /// Target BPM for perceptual weighting (center of log-Gaussian distribution).
    /// Used by <see cref="CombFilterTempoDetector"/> to favor "human" tempos.
    /// Default: 110.0 BPM (typical pop/rock tempo).
    /// </summary>
    public double TargetBpm { get; init; } = 110.0;

    /// <summary>
    /// Width of perceptual weighting distribution in BPM (FWHM).
    /// Used by <see cref="CombFilterTempoDetector"/>.
    /// Default: 30.0 BPM.
    /// </summary>
    public double WidthBpm { get; init; } = 30.0;

    /// <summary>
    /// Number of future onsets to consider when computing all-pairs intervals.
    /// Higher values handle longer rests but increase computation time.
    /// Used by <see cref="CombFilterTempoDetector"/>.
    /// Default: 10.
    /// </summary>
    public int PairwiseWindow { get; init; } = 10;

    /// <summary>
    /// Tolerance for comb filter scoring as a fraction of beat duration.
    /// E.g., 0.05 = 5% of the beat duration. Tighter for fast tempos.
    /// Used by <see cref="CombFilterTempoDetector"/>.
    /// Default: 0.05.
    /// </summary>
    public double ToleranceRatio { get; init; } = 0.05;

    /// <summary>
    /// Tolerance for interval clustering in seconds.
    /// Intervals within this distance are grouped together.
    /// Used by <see cref="CombFilterTempoDetector"/>.
    /// Default: 0.015 seconds (15ms).
    /// </summary>
    public double ClusterToleranceSeconds { get; init; } = 0.015;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when BPM range is invalid.</exception>
    public void Validate()
    {
        if (MinBpm <= 0 || MaxBpm <= MinBpm)
        {
            throw new ArgumentException("Invalid BPM range: MinBpm must be positive and less than MaxBpm");
        }

        if (TargetBpm <= 0 || WidthBpm <= 0)
        {
            throw new ArgumentException("TargetBpm and WidthBpm must be positive");
        }

        if (PairwiseWindow < 1)
        {
            throw new ArgumentException("PairwiseWindow must be at least 1");
        }

        if (ToleranceRatio <= 0 || ToleranceRatio >= 1)
        {
            throw new ArgumentException("ToleranceRatio must be between 0 and 1");
        }

        if (ClusterToleranceSeconds <= 0)
        {
            throw new ArgumentException("ClusterToleranceSeconds must be positive");
        }
    }
}
