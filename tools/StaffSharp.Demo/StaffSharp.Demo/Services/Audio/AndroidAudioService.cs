using System;
using System.Collections.Generic;
using System.Text;

namespace StaffSharp.Demo.Services.Audio;

#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Media;
using Avalonia.Threading;

namespace NoteNet.Demo.Services;

/// <summary>
/// Android-specific audio service implementation using AudioTrack and AudioRecord.
/// </summary>
public sealed class AndroidAudioService : IAudioService
{
    private AudioTrack? _audioTrack;
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

    public event Action<PlaybackState>? PlaybackStateChanged;
    public event Action<TimeSpan>? PositionChanged;
    public event Action<float[]>? RecordingComplete;

    public bool IsPlaying { get; private set; }
    public bool IsRecording { get; private set; }
    public TimeSpan Duration => _playbackBuffer != null && _sampleRate > 0
        ? TimeSpan.FromSeconds((double)_playbackBuffer.Length / _channels / _sampleRate)
        : TimeSpan.Zero;
    public TimeSpan Position => _playbackBuffer != null && _sampleRate > 0
        ? TimeSpan.FromSeconds((double)_playbackPosition / _channels / _sampleRate)
        : TimeSpan.Zero;

    public void PlayAudioBuffer(NoteNet.Core.AudioBuffer audioBuffer)
    {
        Stop();

        _playbackBuffer = audioBuffer.Samples.ToArray();
        _sampleRate = audioBuffer.SampleRate;
        _channels = audioBuffer.Channels;
        _playbackPosition = 0;

        StartPlayback();
    }

    public void PlaySamples(float[] samples, int sampleRate, int channels)
    {
        Stop();

        _playbackBuffer = samples;
        _sampleRate = sampleRate;
        _channels = channels;
        _playbackPosition = 0;

        StartPlayback();
    }

    private void StartPlayback()
    {
        if (_playbackBuffer == null) return;

        var channelConfig = _channels == 1 ? ChannelOut.Mono : ChannelOut.Stereo;
        var minBufferSize = AudioTrack.GetMinBufferSize(
            _sampleRate,
            channelConfig,
            Encoding.PcmFloat);

        var bufferSize = Math.Max(minBufferSize, 4096);

        var audioAttributes = new AudioAttributes.Builder()!
            .SetUsage(AudioUsageKind.Media)!
            .SetContentType(AudioContentType.Music)!
            .Build();

        var audioFormat = new AudioFormat.Builder()!
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

    public void Pause()
    {
        if (_audioTrack != null && IsPlaying)
        {
            _audioTrack.Pause();
            IsPlaying = false;
            PlaybackStateChanged?.Invoke(PlaybackState.Paused);
        }
    }

    public void Resume()
    {
        if (_audioTrack != null && !IsPlaying)
        {
            _audioTrack.Play();
            IsPlaying = true;
            PlaybackStateChanged?.Invoke(PlaybackState.Playing);
        }
    }

    public void Stop()
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
        if (_playbackBuffer == null || _sampleRate <= 0) return;

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
            _audioRecord?.Release();
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

    public float[]? StopRecording()
    {
        if (!IsRecording || _audioRecord == null)
            return null;

        IsRecording = false;
        _recordingCts?.Cancel();
        _recordingTask?.Wait(1000);

        try
        {
            _audioRecord?.Stop();
            _audioRecord?.Release();
        }
        catch { }

        _audioRecord = null;

        float[]? result;
        lock (_recordingLock)
        {
            result = _recordingBuffer?.ToArray();
            _recordingBuffer = null;
        }

        if (result != null)
        {
            RecordingComplete?.Invoke(result);
        }

        return result;
    }

    public NoteNet.Core.AudioBuffer? GetRecordedAudioBufferAndStop()
    {
        var samples = StopRecording();
        if (samples == null || samples.Length == 0)
            return null;

        return new NoteNet.Core.AudioBuffer(samples, _sampleRate, _channels);
    }

    public float[] GetRecordedAudioBuffer()
    {
        lock (_recordingLock)
        {
            return _recordingBuffer?.ToArray() ?? Array.Empty<float>();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Stop();
        StopRecording();
    }
}
#endif
