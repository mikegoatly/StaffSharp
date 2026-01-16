using Android.Media;

using Avalonia.Threading;

using Encoding = Android.Media.Encoding;

namespace StaffSharp.Demo.Services.Audio;

/// <summary>
/// Android-specific audio service implementation using AudioTrack and AudioRecord.
/// </summary>
public sealed class AndroidAudioService : IAudioService, IDisposable
{
#pragma warning disable CA2213 // Disposable fields should be disposed - False positive?
    private AudioTrack? _audioTrack;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private AudioRecord? _audioRecord;
    private float[]? _playbackBuffer;
    private int _playbackPosition;
    private int _sampleRate;
    private int _channels;
    private bool _isDisposed;
    private CancellationTokenSource? _playbackCts;
    private CancellationTokenSource? _recordingCts;
    private Task? _playbackTask;
    private Task? _recordingTask;

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

        var channelConfig = _channels == 1 ? ChannelOut.Mono : ChannelOut.Stereo;
        var minBufferSize = AudioTrack.GetMinBufferSize(
            _sampleRate,
            channelConfig,
            Encoding.PcmFloat);

        var bufferSize = Math.Max(minBufferSize, 4096);

        using var audioAttBuilder = new AudioAttributes.Builder();
        var audioAttributes = audioAttBuilder
            .SetUsage(AudioUsageKind.Media)!
            .SetContentType(AudioContentType.Music)!
            .Build();

        using var audioFmtBuilder = new AudioFormat.Builder();
        var audioFormat = audioFmtBuilder
            .SetEncoding(Encoding.PcmFloat)!
            .SetSampleRate(_sampleRate)!
            .SetChannelMask(channelConfig)!
            .Build();

        _audioTrack = new AudioTrack(
            audioAttributes!,
            audioFormat!,
            bufferSize,
            AudioTrackMode.Stream,
            0);

        IsPlaying = true;
        PlaybackStateChanged?.Invoke(PlaybackState.Playing);

        _playbackCts = new CancellationTokenSource();
        var token = _playbackCts.Token;

        _playbackTask = Task.Run(() =>
        {
            _audioTrack?.Play();

            var chunkSize = 1024;
            while (IsPlaying && !token.IsCancellationRequested && _playbackBuffer != null)
            {
                var samplesRemaining = _playbackBuffer.Length - _playbackPosition;
                if (samplesRemaining <= 0)
                {
                    IsPlaying = false;
                    Dispatcher.UIThread.Post(() => PlaybackStateChanged?.Invoke(PlaybackState.Stopped));
                    break;
                }

                var samplesToWrite = Math.Min(chunkSize, samplesRemaining);
                var written = _audioTrack?.Write(_playbackBuffer, _playbackPosition, samplesToWrite, WriteMode.Blocking) ?? 0;

                if (written > 0)
                {
                    _playbackPosition += written;
                    Dispatcher.UIThread.Post(() => PositionChanged?.Invoke(Position));
                }
                else
                {
                    Thread.Sleep(10);
                }
            }

            _audioTrack?.Stop();
            _audioTrack?.Release();
            _audioTrack = null;
        }, token);
    }

    public void PausePlayback()
    {
        if (_audioTrack != null && IsPlaying)
        {
            _audioTrack.Pause();
            IsPlaying = false;
            PlaybackStateChanged?.Invoke(PlaybackState.Paused);
        }
    }

    public void ResumePlayback()
    {
        if (_audioTrack != null && !IsPlaying)
        {
            _audioTrack.Play();
            IsPlaying = true;
            PlaybackStateChanged?.Invoke(PlaybackState.Playing);
        }
    }

    public void StopPlayback()
    {
        if (_playbackCts != null)
        {
            _playbackCts.Cancel();
            _playbackTask?.Wait(1000);
        }

        if (_audioTrack != null)
        {
            try
            {
                _audioTrack.Stop();
                _audioTrack.Release();
            }
            catch { }
            _audioTrack = null;
        }

        IsPlaying = false;
        _playbackPosition = 0;
        PlaybackStateChanged?.Invoke(PlaybackState.Stopped);
    }

    public void Seek(TimeSpan position)
    {
        if (_playbackBuffer == null || _sampleRate <= 0)
        {
            return;
        }

        var sample = (int)(position.TotalSeconds * _sampleRate * _channels);
        _playbackPosition = Math.Clamp(sample, 0, _playbackBuffer.Length);
        PositionChanged?.Invoke(Position);
    }

    public void StartRecording(int sampleRate = 44100, int channels = 1)
    {
        // Check for microphone permission on Android
        if (PermissionHelper.CheckRecordAudioPermission?.Invoke() == false)
        {
            // Request permission and start recording on success
            PermissionHelper.RequestRecordAudioPermission?.Invoke(granted =>
            {
                if (granted)
                {
                    Dispatcher.UIThread.Post(() => StartRecordingInternal(sampleRate, channels));
                }
            });
            return;
        }

        StartRecordingInternal(sampleRate, channels);
    }

    private void StartRecordingInternal(int sampleRate, int channels)
    {
        StopRecording();

        _sampleRate = sampleRate;
        _channels = channels;
        _recordingBuffer = new List<float>();

        var channelConfig = channels == 1 ? ChannelIn.Mono : ChannelIn.Stereo;
        var minBufferSize = AudioRecord.GetMinBufferSize(
            sampleRate,
            channelConfig,
            Encoding.PcmFloat);

        var bufferSize = Math.Max(minBufferSize, 4096);

        _audioRecord = new AudioRecord(
            AudioSource.Mic,
            sampleRate,
            channelConfig,
            Encoding.PcmFloat,
            bufferSize);

        if (_audioRecord.State != State.Initialized)
        {
            _audioRecord.Release();
            _audioRecord = null;
            return;
        }

        _recordingCts = new CancellationTokenSource();
        var token = _recordingCts.Token;

        IsRecording = true;
        _audioRecord.StartRecording();

        _recordingTask = Task.Run(() =>
        {
            var buffer = new float[1024];
            while (IsRecording && !token.IsCancellationRequested)
            {
                var read = _audioRecord?.Read(buffer, 0, buffer.Length, 0) ?? 0;
                if (read > 0)
                {
                    lock (_recordingLock)
                    {
                        var samples = new float[read];
                        Array.Copy(buffer, samples, read);
                        _recordingBuffer?.AddRange(samples);
                    }
                }
                else if (read == 0)
                {
                    Thread.Sleep(10);
                }
            }
        }, token);
    }

    public AudioBuffer? StopRecording()
    {
        if (!IsRecording || _audioRecord == null)
        {
            return null;
        }

        IsRecording = false;
        _recordingCts?.Cancel();
        _recordingTask?.Wait(1000);

        try
        {
            _audioRecord.Stop();
            _audioRecord.Release();
        }
        catch { }

        _audioRecord = null;

        AudioBuffer? result;
        lock (_recordingLock)
        {
            result = _recordingBuffer != null ? new AudioBuffer(_recordingBuffer.ToArray(), _sampleRate, _channels) : null;
            _recordingBuffer = null;
        }

        return result;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        StopPlayback();
        StopRecording();

        _audioTrack?.Dispose();
        _audioRecord?.Dispose();
        _playbackCts?.Dispose();
        _recordingCts?.Dispose();

        _isDisposed = true;
    }
}
