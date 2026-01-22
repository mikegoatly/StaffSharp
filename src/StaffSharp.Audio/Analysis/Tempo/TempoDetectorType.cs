namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// Specifies the tempo detection algorithm to use.
/// </summary>
public enum TempoDetectorType
{
    /// <summary>
    /// Comb filter tempo detector with all-pairs IOI analysis and phase detection.
    /// Best for music with syncopation, rests, and complex rhythms.
    /// Default option.
    /// </summary>
    CombFilter,

    /// <summary>
    /// Simple inter-onset interval tempo detector using median clustering.
    /// Faster but less robust to syncopation and rests.
    /// </summary>
    InterOnsetInterval
}
