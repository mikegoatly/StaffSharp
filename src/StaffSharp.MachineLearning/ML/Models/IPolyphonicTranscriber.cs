namespace StaffSharp.MachineLearning.ML.Models;

using StaffSharp.Audio;

/// <summary>
/// Interface for polyphonic music transcription models.
/// </summary>
public interface IPolyphonicTranscriber
{
    /// <summary>
    /// Transcribes polyphonic audio to piano roll representation.
    /// </summary>
    /// <param name="audio">The audio buffer to transcribe.</param>
    /// <returns>Transcription result containing piano roll, onsets, and velocities.</returns>
    PolyphonicTranscriptionResult Transcribe(AudioBuffer audio);
}
