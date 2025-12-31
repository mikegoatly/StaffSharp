using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// Interface for tempo detection algorithms.
/// </summary>
public interface ITempoDetector
{
    /// <summary>
    /// Detects tempo from onset times and returns a TempoMap.
    /// Simple implementations return a single tempo at beat 0.
    /// Advanced implementations can detect tempo changes throughout the piece.
    /// </summary>
    /// <param name="onsetTimes">Array of onset times in seconds.</param>
    /// <returns>TempoMap with detected tempo(s), or null if tempo cannot be determined.</returns>
    TempoMap? DetectTempo(ReadOnlySpan<double> onsetTimes);
}
