namespace StaffSharp.Audio.Analysis.Pitch;

/// <summary>
/// Result from pitch detection, including pitch candidates and voicing probability.
/// </summary>
public readonly record struct PitchDetectionResult
{
    /// <summary>
    /// Initializes a new pitch detection result with candidates and voicing probability.
    /// </summary>
    /// <param name="candidates">List of pitch candidates sorted by probability (highest first).</param>
    /// <param name="voicingProbability">Probability that the frame is voiced [0.0, 1.0].</param>
    public PitchDetectionResult(IReadOnlyList<PitchCandidate> candidates, float voicingProbability)
    {
        Candidates = candidates;
        VoicingProbability = voicingProbability;
    }

    public static PitchDetectionResult Unpitched { get; } = new PitchDetectionResult([], 0.0f);

    /// <summary>
    /// All pitch candidates detected, sorted by probability (highest first).
    /// Empty list if no pitch detected.
    /// </summary>
    public IReadOnlyList<PitchCandidate> Candidates { get; } = [];

    /// <summary>
    /// Probability that the frame is voiced (contains pitch) [0.0, 1.0].
    /// </summary>
    public float VoicingProbability { get; }

    /// <summary>
    /// The most likely pitch candidate, or default if no candidates.
    /// </summary>
    public PitchCandidate BestCandidate => Candidates?.Count > 0 ? Candidates[0] : default;

    /// <summary>
    /// Detected pitch in Hz. 0 if no pitch detected.
    /// Convenience property returning the frequency of the best candidate.
    /// </summary>
    public double FrequencyHz => BestCandidate.FrequencyHz;

    /// <summary>
    /// Confidence score [0.0, 1.0]. Higher is more confident.
    /// Convenience property returning the probability of the best candidate.
    /// </summary>
    public float Confidence => BestCandidate.Probability;

    /// <summary>
    /// Whether a pitch was successfully detected.
    /// </summary>
    public bool IsPitched => FrequencyHz > 0 && Confidence > 0;
}
