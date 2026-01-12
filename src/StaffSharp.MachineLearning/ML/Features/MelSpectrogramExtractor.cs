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

        // 1. Resample audio to target sample rate if needed
        var processedAudio = audio.Resample(_options.SampleRate);

        // 2. Convert to mono if needed
        if (processedAudio.Channels > 1)
        {
            processedAudio = processedAudio.ToMono();
        }

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
            for (int i = 0; i < _fftBins; i++)
            {
                stft[frameIdx, i] = complexBuffer[i];
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

        // Convert min/max frequencies to mel scale
        var minMel = HzToMel(_options.MinFrequency);
        var maxMel = HzToMel(_options.MaxFrequency);

        // Create mel bin edges (linearly spaced in mel scale)
        var melPoints = new float[_options.MelBins + 2];
        var melStep = (maxMel - minMel) / (_options.MelBins + 1);
        for (int i = 0; i < melPoints.Length; i++)
        {
            melPoints[i] = minMel + (i * melStep);
        }

        // Convert mel points back to Hz and then to FFT bin indices
        var freqPoints = new float[melPoints.Length];
        var binPoints = new int[melPoints.Length];
        for (int i = 0; i < melPoints.Length; i++)
        {
            freqPoints[i] = MelToHz(melPoints[i]);
            binPoints[i] = (int)(freqPoints[i] * _options.FrameSize / _options.SampleRate);

            // Clamp to valid FFT bin range
            if (binPoints[i] >= _fftBins)
                binPoints[i] = _fftBins - 1;
        }

        // Create triangular filters
        for (int m = 0; m < _options.MelBins; m++)
        {
            var leftBin = binPoints[m];
            var centerBin = binPoints[m + 1];
            var rightBin = binPoints[m + 2];

            // Rising slope from left to center
            for (int bin = leftBin; bin < centerBin && bin < _fftBins; bin++)
            {
                if (centerBin > leftBin)
                {
                    filterbank[m, bin] = (float)(bin - leftBin) / (centerBin - leftBin);
                }
            }

            // Falling slope from center to right
            for (int bin = centerBin; bin < rightBin && bin < _fftBins; bin++)
            {
                if (rightBin > centerBin)
                {
                    filterbank[m, bin] = (float)(rightBin - bin) / (rightBin - centerBin);
                }
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
