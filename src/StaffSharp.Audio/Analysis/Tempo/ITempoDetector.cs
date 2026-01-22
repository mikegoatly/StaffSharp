using StaffSharp.Audio.Pipeline;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// Interface for tempo detection algorithms.
/// </summary>
public interface ITempoDetector
{
    /// <summary>
    /// Detects tempo from onset times and returns tempo changes.
    /// Simple implementations return a single tempo at beat 0.
    /// Advanced implementations can detect tempo changes throughout the piece.
    /// </summary>
    /// <param name="progress">Pipeline progress and diagnostics collector.</param>
    /// <param name="onsetTimes">Array of onset times in seconds.</param>
    IReadOnlyList<TempoChange> DetectTempo(PipelineProgress progress, ReadOnlySpan<double> onsetTimes);
}
