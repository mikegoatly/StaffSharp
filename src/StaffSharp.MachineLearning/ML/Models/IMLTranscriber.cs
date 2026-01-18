namespace StaffSharp.MachineLearning.ML.Models;

using StaffSharp.Audio;
using StaffSharp.Audio.Pipeline;

using System.Threading.Tasks;

/// <summary>
/// Interface for polyphonic music transcription models.
/// </summary>
public interface IMLTranscriber : IDisposable
{
    /// <summary>
    /// Transcribes polyphonic audio to piano roll representation.
    /// </summary>
    /// <param name="progress">Pipeline progress and diagnostics collector.</param>
    /// <param name="audio">The audio buffer to transcribe.</param>
    /// <returns>Transcription result containing piano roll, onsets, and velocities.</returns>
    Task<PolyphonicTranscriptionResult> TranscribeAsync(PipelineProgress progress, AudioBuffer audio);
}
