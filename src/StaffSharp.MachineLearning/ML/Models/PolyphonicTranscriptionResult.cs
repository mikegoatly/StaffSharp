namespace StaffSharp.MachineLearning.ML.Models;

/// <summary>
/// Result of polyphonic transcription containing piano roll, onset roll, offset roll, and velocities.
/// </summary>
/// <param name="PianoRoll">
/// Frame activation matrix (time_frames, 88 piano keys).
/// Values in range [0, 1] indicate probability that each note is active in each frame.
/// Piano keys map to MIDI notes 21 (A0) through 108 (C8).
/// </param>
/// <param name="OnsetRoll">
/// Note onset matrix (time_frames, 88 piano keys).
/// Values in range [0, 1] indicate probability of note onset in each frame.
/// </param>
/// <param name="OffsetRoll">
/// Note offset matrix (time_frames, 88 piano keys).
/// Values in range [0, 1] indicate probability of note offset (release) in each frame.
/// </param>
/// <param name="VelocityRoll">
/// Note velocity matrix (time_frames, 88 piano keys).
/// Values in range [0, 1] represent normalized velocity.
/// Non-zero values typically only appear at onset frames.
/// </param>
/// <param name="FrameRate">
/// Number of frames per second (Hz).
/// Computed as SampleRate / HopSize.
/// </param>
/// <param name="SampleRate">
/// Audio sample rate used for feature extraction (Hz).
/// </param>
public sealed record PolyphonicTranscriptionResult(
    float[,] PianoRoll,
    float[,] OnsetRoll,
    float[,] OffsetRoll,
    float[,] VelocityRoll,
    int FrameRate,
    int SampleRate)
{
    /// <summary>
    /// Number of time frames in the transcription.
    /// </summary>
    public int NumFrames => PianoRoll.GetLength(0);

    /// <summary>
    /// Duration of the transcription in seconds.
    /// </summary>
    public double DurationSeconds => (double)NumFrames / FrameRate;
}
