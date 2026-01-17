using StaffSharp.Audio.Pipeline;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Meter;

/// <summary>
/// Interface for time signature detection algorithms.
/// </summary>
public interface ITimeSignatureDetector
{
    /// <summary>
    /// Detects time signature(s) from onset times.
    /// Simple implementations return a single TimeSignatureChange at beat 0.
    /// Advanced implementations can detect meter changes throughout the piece.
    /// </summary>
    /// <param name="progress">Progress and diagnostics reporting.</param>
    /// <param name="onsetTimes">Array of onset times in seconds.</param>
    /// <param name="estimatedTempo">Optional tempo hint in BPM to aid detection.</param>
    /// <returns>List of time signature changes, must include at least one at beat 0.</returns>
    IReadOnlyList<TimeSignatureChange> DetectTimeSignatures(
        PipelineProgress progress,
        ReadOnlySpan<double> onsetTimes,
        double? estimatedTempo = null);
}
