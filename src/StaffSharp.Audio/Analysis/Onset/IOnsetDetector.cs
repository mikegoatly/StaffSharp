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
    /// <returns>Array of onset times in seconds.</returns>
    double[] DetectOnsets(ReadOnlySpan<float> buffer, int sampleRate);
}
