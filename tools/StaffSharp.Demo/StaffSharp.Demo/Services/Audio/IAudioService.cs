namespace StaffSharp.Demo.Services.Audio;

/// <summary>
/// Cross-platform audio service interface for playback and recording.
/// </summary>
public interface IAudioService : IDisposable
{
    Action<PlaybackState>? PlaybackStateChanged { get; set; }
    Action<TimeSpan>? PositionChanged { get; set; }

    bool IsPlaying { get; }
    bool IsRecording { get; }
    TimeSpan Duration { get; }
    TimeSpan Position { get; }

    void PlayAudioBuffer(AudioBuffer audioBuffer);
    void PausePlayback();
    void ResumePlayback();
    void StopPlayback();
    void Seek(TimeSpan position);

    void StartRecording(int sampleRate = 44100, int channels = 1);
    AudioBuffer? StopRecording();
}
