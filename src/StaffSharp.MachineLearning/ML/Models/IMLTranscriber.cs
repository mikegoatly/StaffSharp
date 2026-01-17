namespace StaffSharp.MachineLearning.ML.Models;

using StaffSharp.Audio;
using System.Threading.Tasks;

/// <summary>
/// Interface for polyphonic music transcription models.
/// </summary>
public interface IMLTranscriber : IDisposable
{
    /// <summary>
    /// Transcribes polyphonic audio to piano roll representation.
    /// </summary>
    /// <param name="audio">The audio buffer to transcribe.</param>
    /// <returns>Transcription result containing piano roll, onsets, and velocities.</returns>
    Task<PolyphonicTranscriptionResult> TranscribeAsync(AudioBuffer audio);
}
