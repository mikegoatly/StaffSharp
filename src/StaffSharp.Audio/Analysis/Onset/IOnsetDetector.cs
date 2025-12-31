namespace StaffSharp.Audio.Analysis.Onset;

/// <summary>
/// Interface for onset detection algorithms.
/// Onsets mark the beginning of musical notes or events.
/// </summary>
public interface IOnsetDetector
{
    /// <summary>
    /// Detects onset times in an audio buffer.
    /// </summary>
    /// <param name="buffer">Audio buffer (mono, normalized).</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <param name="startTimeOffset">Optional time offset in seconds to add to all detected onset times.
    /// Used when processing a slice of audio to preserve absolute timing relative to the original recording.</param>
    /// <returns>Array of onset times in seconds (with offset applied if provided).</returns>
    double[] DetectOnsets(ReadOnlySpan<float> buffer, int sampleRate, double startTimeOffset = 0.0);
}
