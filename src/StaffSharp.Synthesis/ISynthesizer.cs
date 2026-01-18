using StaffSharp.Audio;
using StaffSharp.Notation;

namespace StaffSharp.Synthesis;

/// <summary>
/// Synthesizes musical scores into audio samples.
/// </summary>
public interface ISynthesizer
{
    /// <summary>
    /// Synthesizes a score to an AudioBuffer.
    /// </summary>
    /// <param name="score">The musical score to synthesize.</param>
    /// <param name="sampleRate">Sample rate in Hz (default: 44100).</param>
    /// <returns>An AudioBuffer containing the synthesized audio.</returns>
    AudioBuffer Synthesize(NotationScore score, int sampleRate = 44100);
}
