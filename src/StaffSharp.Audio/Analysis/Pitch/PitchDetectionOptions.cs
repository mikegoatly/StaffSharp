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
    /// 
    /// For YIN: The threshold for accepting the first local minimum in the CMNDF.
    /// Lower values are more conservative (may miss weak pitches), higher values are more permissive.
    /// 
    /// For pYIN: The minimum valley depth for a candidate to be considered.
    /// Only CMNDF local minima with values below this threshold are included as pitch candidates.
    /// 
    /// Default: 0.15
    /// Valid range: (0, 1)
    /// </summary>
    public float Threshold { get; init; } = 0.15f;

    /// <summary>
    /// Gets or initializes the Boltzmann temperature parameter for pYIN.
    /// Controls the shape of the probability distribution over pitch candidates.
    /// Higher values create a more uniform distribution, lower values favor shorter periods (higher frequencies) more strongly.
    /// Default: 2.0 (from librosa/pYIN paper).
    /// </summary>
    public double BoltzmannTemperature { get; init; } = 2.0;

    /// <summary>
    /// Gets or initializes the first parameter of the beta distribution prior for pYIN voicing probability.
    /// Used in conjunction with BetaDist2 to compute the probability that a frame is voiced.
    /// Default: 2.0 (from librosa/pYIN paper).
    /// </summary>
    public double BetaDist1 { get; init; } = 2.0;

    /// <summary>
    /// Gets or initializes the second parameter of the beta distribution prior for pYIN voicing probability.
    /// Used in conjunction with BetaDist1 to compute the probability that a frame is voiced.
    /// Default: 18.0 (from librosa/pYIN paper).
    /// </summary>
    public double BetaDist2 { get; init; } = 18.0;

    /// <summary>
    /// Gets or initializes the probability threshold for pruning pYIN candidates.
    /// Candidates with probability below this threshold are discarded.
    /// Default: 0.01 (1%).
    /// Valid range: [0, 1]
    /// </summary>
    public float CandidateThreshold { get; init; } = 0.01f;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when frequency range is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold or other parameters are out of valid range.</exception>
    public void Validate()
    {
        if (MinFrequency <= 0 || MaxFrequency <= MinFrequency)
        {
            throw new ArgumentException("Invalid frequency range: MinFrequency must be positive and less than MaxFrequency");
        }

        if (Threshold <= 0 || Threshold >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Threshold), "Threshold must be in (0, 1)");
        }

        if (BoltzmannTemperature <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BoltzmannTemperature), "BoltzmannTemperature must be positive");
        }

        if (BetaDist1 <= 0 || BetaDist2 <= 0)
        {
            throw new ArgumentOutOfRangeException($"{nameof(BetaDist1)} and {nameof(BetaDist2)} must be positive");
        }

        if (CandidateThreshold < 0 || CandidateThreshold > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(CandidateThreshold), "CandidateThreshold must be in [0, 1]");
        }
    }
}
