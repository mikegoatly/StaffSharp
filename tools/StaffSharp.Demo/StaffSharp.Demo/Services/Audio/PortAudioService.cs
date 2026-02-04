using System.Runtime.InteropServices;

using PortAudioSharp;

using StaffSharp.Audio;

namespace StaffSharp.Demo.Services.Audio;

/// <summary>
/// Audio service implementation using PortAudio for playback and recording.
/// </summary>
public sealed class PortAudioService : IAudioService, IDisposable
{
    private PortAudioSharp.Stream? _playbackStream;
    private PortAudioSharp.Stream? _recordingStream;
    private float[]? _playbackBuffer;
    private int _playbackPosition;
    private int _sampleRate;
    private int _channels;
    private bool _isDisposed;
    private CancellationTokenSource? _recordingCts;

    // Recording buffer
    private List<float>? _recordingBuffer;
    private readonly object _recordingLock = new();

    public Action<PlaybackState>? PlaybackStateChanged { get; set; }
    public Action<TimeSpan>? PositionChanged { get; set; }

    public bool IsPlaying { get; private set; }
    public bool IsRecording { get; private set; }
    public TimeSpan Duration => _playbackBuffer != null && _sampleRate > 0
        ? TimeSpan.FromSeconds((double)_playbackBuffer.Length / _channels / _sampleRate)
        : TimeSpan.Zero;
    public TimeSpan Position => _playbackBuffer != null && _sampleRate > 0
        ? TimeSpan.FromSeconds((double)_playbackPosition / _channels / _sampleRate)
        : TimeSpan.Zero;

    static PortAudioService()
    {
        // Initialize PortAudio once
        PortAudio.Initialize();
    }

    /// <summary>
    /// Plays audio from an AudioBuffer.
    /// </summary>
    public void PlayAudioBuffer(AudioBuffer audioBuffer)
    {
        StopPlayback();

        _playbackBuffer = audioBuffer.Samples.ToArray();
        _sampleRate = audioBuffer.SampleRate;
        _channels = audioBuffer.Channels;
        _playbackPosition = 0;

        StartPlayback();
    }

    private void StartPlayback()
    {
        if (_playbackBuffer == null)
        {
            return;
        }

        var outputParams = new StreamParameters
        {
            device = PortAudio.DefaultOutputDevice,
            channelCount = _channels,
            sampleFormat = SampleFormat.Float32,
            suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultOutputDevice).defaultLowOutputLatency
        };

        _playbackStream = new PortAudioSharp.Stream(
            inParams: null,
            outParams: outputParams,
            sampleRate: _sampleRate,
            framesPerBuffer: 1024,
            streamFlags: StreamFlags.ClipOff,
            callback: PlaybackCallback,
            userData: IntPtr.Zero
        );

        _playbackStream.Start();
        IsPlaying = true;
        PlaybackStateChanged?.Invoke(PlaybackState.Playing);

        // Start position tracking
        Task.Run(async () =>
        {
            while (IsPlaying && _playbackStream != null)
            {
                PositionChanged?.Invoke(Position);
                await Task.Delay(100);
            }
        });
    }

    private StreamCallbackResult PlaybackCallback(
        IntPtr input,
        IntPtr output,
        uint frameCount,
        ref StreamCallbackTimeInfo timeInfo,
        StreamCallbackFlags statusFlags,
        IntPtr userData)
    {
        if (_playbackBuffer == null)
            return StreamCallbackResult.Complete;

        var samplesToWrite = (int)frameCount * _channels;
        var samplesRemaining = _playbackBuffer.Length - _playbackPosition;

        if (samplesRemaining <= 0)
        {
            IsPlaying = false;
            PlaybackStateChanged?.Invoke(PlaybackState.Stopped);
            return StreamCallbackResult.Complete;
        }

        var samplesToCopy = Math.Min(samplesToWrite, samplesRemaining);
        Marshal.Copy(_playbackBuffer, _playbackPosition, output, samplesToCopy);
        _playbackPosition += samplesToCopy;

        // Zero-fill if we ran out of samples
        if (samplesToCopy < samplesToWrite)
        {
            var zeroCount = samplesToWrite - samplesToCopy;
            var zeroBuffer = new float[zeroCount];
            Marshal.Copy(zeroBuffer, 0, output + samplesToCopy * sizeof(float), zeroCount);
        }

        return StreamCallbackResult.Continue;
    }

    /// <summary>
    /// Starts recording audio from the default input device.
    /// </summary>
    public void StartRecording(int sampleRate = 44100, int channels = 1)
    {
        StopRecording();

        _sampleRate = sampleRate;
        _channels = channels;
        _recordingBuffer = [];
        _recordingCts = new CancellationTokenSource();

        var inputParams = new StreamParameters
        {
            device = PortAudio.DefaultInputDevice,
            channelCount = channels,
            sampleFormat = SampleFormat.Float32,
            suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultInputDevice).defaultLowInputLatency
        };

        _recordingStream = new PortAudioSharp.Stream(
            inParams: inputParams,
            outParams: null,
            sampleRate: sampleRate,
            framesPerBuffer: 1024,
            streamFlags: StreamFlags.ClipOff,
            callback: RecordingCallback,
            userData: IntPtr.Zero
        );

        _recordingStream.Start();
        IsRecording = true;
    }

    private StreamCallbackResult RecordingCallback(
        IntPtr input,
        IntPtr output,
        uint frameCount,
        ref StreamCallbackTimeInfo timeInfo,
        StreamCallbackFlags statusFlags,
        IntPtr userData)
    {
        if (_recordingCts?.IsCancellationRequested == true)
        {
            return StreamCallbackResult.Complete;
        }

        var sampleCount = (int)frameCount * _channels;
        var samples = new float[sampleCount];
        Marshal.Copy(input, samples, 0, sampleCount);

        lock (_recordingLock)
        {
            _recordingBuffer?.AddRange(samples);
        }

        return StreamCallbackResult.Continue;
    }

    /// <summary>
    /// Stops recording and returns the recorded samples.
    /// </summary>
    public AudioBuffer? StopRecording()
    {
        if (!IsRecording || _recordingStream == null)
        {
            return null;
        }

        _recordingCts?.Cancel();
        IsRecording = false;

        try
        {
            _recordingStream.Stop();
            _recordingStream.Dispose();
        }
        catch { }

        _recordingStream = null;

        AudioBuffer? result;
        lock (_recordingLock)
        {
            result = _recordingBuffer != null ? new AudioBuffer([.. _recordingBuffer], _sampleRate, _channels) : null;
            _recordingBuffer = null;
        }

        return result;
    }

    public void PausePlayback()
    {
        _playbackStream?.Stop();
        IsPlaying = false;
        PlaybackStateChanged?.Invoke(PlaybackState.Paused);
    }

    public void ResumePlayback()
    {
        _playbackStream?.Start();
        IsPlaying = true;
        PlaybackStateChanged?.Invoke(PlaybackState.Playing);
    }

    public void StopPlayback()
    {
        if (_playbackStream != null)
        {
            try
            {
                _playbackStream.Stop();
                _playbackStream.Dispose();
            }
            catch { }

            _playbackStream = null;
        }

        IsPlaying = false;
        _playbackPosition = 0;
        PlaybackStateChanged?.Invoke(PlaybackState.Stopped);
    }

    public void Seek(TimeSpan position)
    {
        if (_playbackBuffer == null || _sampleRate <= 0) return;

        var sample = (int)(position.TotalSeconds * _sampleRate * _channels);
        _playbackPosition = Math.Clamp(sample, 0, _playbackBuffer.Length);
        PositionChanged?.Invoke(Position);
    }

    /// <summary>
    /// Lists available audio input devices.
    /// </summary>
    public static IEnumerable<(int Index, string Name)> GetInputDevices()
    {
        var deviceCount = PortAudio.DeviceCount;
        for (int i = 0; i < deviceCount; i++)
        {
            var info = PortAudio.GetDeviceInfo(i);
            if (info.maxInputChannels > 0)
            {
                yield return (i, info.name);
            }
        }
    }

    /// <summary>
    /// Lists available audio output devices.
    /// </summary>
    public static IEnumerable<(int Index, string Name)> GetOutputDevices()
    {
        var deviceCount = PortAudio.DeviceCount;
        for (int i = 0; i < deviceCount; i++)
        {
            var info = PortAudio.GetDeviceInfo(i);
            if (info.maxOutputChannels > 0)
            {
                yield return (i, info.name);
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        StopPlayback();
        StopRecording();

        _recordingCts?.Dispose();
    }
}
