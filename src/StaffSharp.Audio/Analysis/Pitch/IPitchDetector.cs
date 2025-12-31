namespace StaffSharp.Audio.Analysis.Pitch;

/// <summary>
/// Interface for pitch detection algorithms.
/// </summary>
public interface IPitchDetector
{
    /// <summary>
    /// Detects the fundamental frequency (pitch) of an audio buffer.
    /// </summary>
    /// <param name="buffer">Audio buffer (mono, normalized).</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <returns>Pitch detection result with frequency and confidence.</returns>
    PitchDetectionResult DetectPitch(ReadOnlySpan<float> buffer, int sampleRate);
}
