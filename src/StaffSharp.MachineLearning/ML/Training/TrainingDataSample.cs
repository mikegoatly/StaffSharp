namespace StaffSharp.MachineLearning.ML.Training;

/// <summary>
/// Represents a single training data sample for polyphonic piano transcription.
/// </summary>
public sealed class TrainingDataSample
{
    /// <summary>
    /// Gets the mel spectrogram features with shape (time_frames, mel_bins).
    /// </summary>
    public required float[,] MelSpectrogram { get; init; }

    /// <summary>
    /// Gets the piano roll (active notes) with shape (time_frames, 88).
    /// Values are 1 when a note is active, 0 when silent.
    /// </summary>
    public required float[,] PianoRoll { get; init; }

    /// <summary>
    /// Gets the onset roll with shape (time_frames, 88).
    /// Values are 1 at note onset frames, 0 otherwise.
    /// </summary>
    public required float[,] OnsetRoll { get; init; }

    /// <summary>
    /// Gets the offset roll with shape (time_frames, 88).
    /// Values are 1 at note offset frames, 0 otherwise.
    /// </summary>
    public required float[,] OffsetRoll { get; init; }

    /// <summary>
    /// Gets the velocity roll with shape (time_frames, 88).
    /// Values are normalized velocities (0.0-1.0) at onset frames, 0 otherwise.
    /// </summary>
    public required float[,] VelocityRoll { get; init; }

    /// <summary>
    /// Gets the path to the source audio file (optional metadata).
    /// </summary>
    public string? AudioPath { get; init; }

    /// <summary>
    /// Gets the path to the source MIDI file (optional metadata).
    /// </summary>
    public string? MidiPath { get; init; }
}
