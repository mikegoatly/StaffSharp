using StaffSharp.Audio.Pipeline;

namespace StaffSharp.Audio.Analysis.Boundaries;

/// <summary>
/// Interface for detecting the boundaries of actual musical content in audio.
/// Used to identify and skip leading/trailing silence while preserving absolute timing.
/// </summary>
public interface IAudioBoundaryDetector
{
    /// <summary>
    /// Detects the start and end of actual musical content in an audio buffer.
    /// Returns null if no content is detected (entire buffer is silence).
    /// </summary>
    /// <param name="progress">Progress and diagnostics reporting.</param>
    /// <param name="audio">The audio buffer to analyze.</param>
    /// <returns>Boundaries of the content, or null if no content detected.</returns>
    AudioBoundaries? DetectBoundaries(PipelineProgress progress, AudioBuffer audio);
}
