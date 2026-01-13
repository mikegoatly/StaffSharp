namespace StaffSharp.MachineLearning.ML.Features;

using System.Numerics;

using MathNet.Numerics.IntegralTransforms;

using StaffSharp.Audio;
using StaffSharp.Audio.Numerics;
using StaffSharp.MachineLearning.Options;

/// <summary>
/// Extracts mel spectrogram features from audio.
/// Implementation must match Python training code exactly for correct inference.
/// </summary>
public sealed class MelSpectrogramExtractor : IFeatureExtractor
{
    private readonly MelSpectrogramOptions _options;
    private readonly float[] _window;
    private readonly float[,] _melFilterbank;
    private readonly int _fftBins;

    public MelSpectrogramExtractor(MelSpectrogramOptions? options = null)
    {
        _options = options ?? new MelSpectrogramOptions();
        _window = WindowFunctions.CreateHannWindow(_options.FrameSize);
        _fftBins = (_options.FrameSize / 2) + 1;
        _melFilterbank = CreateMelFilterbank();
    }

    /// <inheritdoc/>
    public float[,] ExtractFeatures(AudioBuffer audio)
    {
        ArgumentNullException.ThrowIfNull(audio);

        // 1. Convert to mono first (matches librosa.load(mono=True) behavior)
        var processedAudio = audio;
        if (processedAudio.Channels > 1)
        {
            processedAudio = processedAudio.ToMono();
        }

        // 2. Resample audio to target sample rate if needed
        processedAudio = processedAudio.Resample(_options.SampleRate);

        // 3. Compute STFT (Short-Time Fourier Transform)
        var stft = ComputeStft(processedAudio);

        // 4. Convert to power spectrogram
        var powerSpec = ComputePowerSpectrogram(stft);

        // 5. Apply mel filterbank
        var melSpec = ApplyMelFilterbank(powerSpec);

        // 6. Apply logarithmic compression
        ApplyLogCompression(melSpec);

        return melSpec;
    }

    private Complex[,] ComputeStft(AudioBuffer audio)
    {
        var samples = audio.Samples.Span;
        var numFrames = ((samples.Length - _options.FrameSize) / _options.HopSize) + 1;

        if (numFrames <= 0)
            throw new ArgumentException("Audio is too short for the specified frame and hop size", nameof(audio));

        var stft = new Complex[numFrames, _fftBins];
        var complexBuffer = new Complex[_options.FrameSize];
        var windowedFrame = new float[_options.FrameSize];

        for (int frameIdx = 0; frameIdx < numFrames; frameIdx++)
        {
            var frameStart = frameIdx * _options.HopSize;

            // Extract and window the frame
            for (int i = 0; i < _options.FrameSize; i++)
            {
                windowedFrame[i] = samples[frameStart + i] * _window[i];
            }

            // Convert to complex for FFT
            for (int i = 0; i < _options.FrameSize; i++)
            {
                complexBuffer[i] = new Complex(windowedFrame[i], 0);
            }

            // Compute FFT
            Fourier.Forward(complexBuffer, FourierOptions.Default);

            // Store only positive frequencies (first half + Nyquist)
            // Note: MathNet.Numerics uses sqrt(N) normalization (unitary), but librosa uses no normalization.
            // We multiply by sqrt(frame size) to match librosa's normalization.
            var scale = MathF.Sqrt(_options.FrameSize);
            for (int i = 0; i < _fftBins; i++)
            {
                stft[frameIdx, i] = complexBuffer[i] * scale;
            }
        }

        return stft;
    }

    private float[,] ComputePowerSpectrogram(Complex[,] stft)
    {
        var numFrames = stft.GetLength(0);
        var powerSpec = new float[numFrames, _fftBins];

        for (int t = 0; t < numFrames; t++)
        {
            for (int f = 0; f < _fftBins; f++)
            {
                var magnitude = stft[t, f].Magnitude;
                powerSpec[t, f] = (float)(magnitude * magnitude);
            }
        }

        return powerSpec;
    }

    private float[,] ApplyMelFilterbank(float[,] powerSpec)
    {
        var numFrames = powerSpec.GetLength(0);
        var melSpec = new float[numFrames, _options.MelBins];

        for (int t = 0; t < numFrames; t++)
        {
            for (int m = 0; m < _options.MelBins; m++)
            {
                float sum = 0;
                for (int f = 0; f < _fftBins; f++)
                {
                    sum += powerSpec[t, f] * _melFilterbank[m, f];
                }
                melSpec[t, m] = sum;
            }
        }

        return melSpec;
    }

    private void ApplyLogCompression(float[,] melSpec)
    {
        var numFrames = melSpec.GetLength(0);
        var constant = _options.LogCompressionConstant;

        for (int t = 0; t < numFrames; t++)
        {
            for (int m = 0; m < _options.MelBins; m++)
            {
                melSpec[t, m] = MathF.Log(1 + (constant * melSpec[t, m]));
            }
        }
    }

    private float[,] CreateMelFilterbank()
    {
        var filterbank = new float[_options.MelBins, _fftBins];
        var nyquist = _options.SampleRate / 2.0f;

        // Validate that max frequency doesn't exceed Nyquist frequency
        if (_options.MaxFrequency > nyquist)
        {
            throw new ArgumentException(
                $"MaxFrequency ({_options.MaxFrequency} Hz) exceeds Nyquist frequency ({nyquist} Hz) " +
                $"for sample rate {_options.SampleRate} Hz");
        }

        // Create FFT bin frequencies (matches librosa.fft_frequencies)
        var fftFreqs = new float[_fftBins];
        for (int i = 0; i < _fftBins; i++)
        {
            fftFreqs[i] = i * _options.SampleRate / (float)_options.FrameSize;
        }

        // Convert min/max frequencies to mel scale
        var minMel = HzToMel(_options.MinFrequency);
        var maxMel = HzToMel(_options.MaxFrequency);

        // Create mel bin edges (linearly spaced in mel scale) - matches librosa.mel_frequencies
        var melFreqs = new float[_options.MelBins + 2];
        var melStep = (maxMel - minMel) / (_options.MelBins + 1);
        for (int i = 0; i < melFreqs.Length; i++)
        {
            melFreqs[i] = MelToHz(minMel + (i * melStep));
        }

        // Compute differences between adjacent mel frequencies (fdiff in librosa)
        var fdiff = new float[melFreqs.Length - 1];
        for (int i = 0; i < fdiff.Length; i++)
        {
            fdiff[i] = melFreqs[i + 1] - melFreqs[i];
        }

        // Create ramps array: outer subtraction of melFreqs and fftFreqs
        // ramps[i, j] = melFreqs[i] - fftFreqs[j]
        var ramps = new float[melFreqs.Length, _fftBins];
        for (int i = 0; i < melFreqs.Length; i++)
        {
            for (int j = 0; j < _fftBins; j++)
            {
                ramps[i, j] = melFreqs[i] - fftFreqs[j];
            }
        }

        // Build triangular filters (matches librosa exactly)
        for (int i = 0; i < _options.MelBins; i++)
        {
            // lower = -ramps[i] / fdiff[i]
            // upper = ramps[i + 2] / fdiff[i + 1]
            // weights[i] = max(0, min(lower, upper))
            for (int j = 0; j < _fftBins; j++)
            {
                float lower = -ramps[i, j] / fdiff[i];
                float upper = ramps[i + 2, j] / fdiff[i + 1];
                filterbank[i, j] = MathF.Max(0, MathF.Min(lower, upper));
            }
        }

        // Apply Slaney normalization (matches librosa's norm='slaney')
        // enorm = 2.0 / (mel_f[2:n_mels+2] - mel_f[:n_mels])
        for (int i = 0; i < _options.MelBins; i++)
        {
            float enorm = 2.0f / (melFreqs[i + 2] - melFreqs[i]);
            for (int j = 0; j < _fftBins; j++)
            {
                filterbank[i, j] *= enorm;
            }
        }

        return filterbank;
    }

    /// <summary>
    /// Converts frequency in Hz to mel scale.
    /// Formula: mel = 2595 * log10(1 + hz / 700)
    /// </summary>
    private static float HzToMel(float hz)
    {
        return 2595.0f * MathF.Log10(1.0f + (hz / 700.0f));
    }

    /// <summary>
    /// Converts mel scale to frequency in Hz.
    /// Formula: hz = 700 * (10^(mel / 2595) - 1)
    /// </summary>
    private static float MelToHz(float mel)
    {
        return 700.0f * (MathF.Pow(10.0f, mel / 2595.0f) - 1.0f);
    }
}
