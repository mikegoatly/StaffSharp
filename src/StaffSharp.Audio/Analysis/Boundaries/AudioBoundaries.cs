namespace StaffSharp.Audio.Analysis.Boundaries;

/// <summary>
/// Represents the detected boundaries of actual musical content in an audio recording.
/// Used to skip silence at the beginning and end while preserving absolute timing.
/// </summary>
/// <param name="StartSample">First sample of actual content (inclusive).</param>
/// <param name="EndSample">Last sample of actual content (exclusive).</param>
/// <param name="SampleRate">Sample rate of the audio, used for time calculations.</param>
/// <param name="LeadingSilence">Duration of silence before content starts.</param>
/// <param name="TrailingSilence">Duration of silence after content ends.</param>
public sealed record AudioBoundaries(
    int StartSample,
    int EndSample,
    int SampleRate,
    TimeSpan LeadingSilence,
    TimeSpan TrailingSilence)
{
    /// <summary>
    /// Gets the duration of the actual content (excluding silence).
    /// </summary>
    public TimeSpan ContentDuration =>
        TimeSpan.FromSeconds((EndSample - StartSample) / (double)SampleRate);

    /// <summary>
    /// Gets the total duration of the recording (including all silence).
    /// </summary>
    public TimeSpan TotalDuration =>
        LeadingSilence + ContentDuration + TrailingSilence;
}
