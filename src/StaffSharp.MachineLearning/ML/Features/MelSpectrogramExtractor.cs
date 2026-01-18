namespace StaffSharp.MachineLearning.ML.Features;

using System.Numerics;
using System.Numerics.Tensors;

using MathNet.Numerics.IntegralTransforms;

using StaffSharp.Audio;
using StaffSharp.Audio.Numerics;
using StaffSharp.Audio.Pipeline;
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
    public float[,] ExtractFeatures(PipelineProgress progress, AudioBuffer audio)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(audio);

        // 1. Convert to mono first
        var processedAudio = audio;
        if (processedAudio.Channels > 1)
        {
            processedAudio = processedAudio.ToMono();
        }

        // Ensure the audio is normalized
        (processedAudio, var normalizationStats) = processedAudio.Normalize();
        progress.EmitDiagnostics("NormalizationStats", normalizationStats);
        progress.EmitDiagnostics("NormalizedWaveform", processedAudio.Samples.ToArray());

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

        progress.EmitDiagnostics("MelSpectrogram", melSpec);

        return melSpec;
    }

    private Complex[,] ComputeStft(AudioBuffer audio)
    {
        var originalSamples = audio.Samples.Span;

        // We pad with zeros so that Frame 0 is centered at Time 0.
        // Without this, features are shifted by (FrameSize/2) / SampleRate seconds (approx 64ms).
        int padLength = _options.FrameSize / 2;
        var paddedLength = originalSamples.Length + (padLength * 2);
        var paddedSamples = new float[paddedLength];

        // Copy original audio into the middle
        originalSamples.CopyTo(paddedSamples.AsSpan().Slice(padLength, originalSamples.Length));
        
        // TODO Consider implementing reflection padding at edges

        // Recalculate numFrames based on PADDED length
        // This formula ensures numFrames * hopSize is roughly equal to originalSamples.Length
        var numFrames = ((paddedLength - _options.FrameSize) / _options.HopSize) + 1;

        if (numFrames <= 0)
        {
            throw new ArgumentException("Audio is too short", nameof(audio));
        }

        var stft = new Complex[numFrames, _fftBins];
        var complexBuffer = new Complex[_options.FrameSize];
        var windowedFrame = new float[_options.FrameSize];

        for (int frameIdx = 0; frameIdx < numFrames; frameIdx++)
        {
            var frameStart = frameIdx * _options.HopSize;

            // Extract and window from PADDED samples
            for (int i = 0; i < _options.FrameSize; i++)
            {
                windowedFrame[i] = paddedSamples[frameStart + i] * _window[i];
            }

            // Convert to complex for FFT
            for (int i = 0; i < _options.FrameSize; i++)
            {
                complexBuffer[i] = new Complex(windowedFrame[i], 0);
            }

            Fourier.Forward(complexBuffer, FourierOptions.Default);

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

        // Use vectorized operations for better performance
        Span<float> realParts = stackalloc float[_fftBins];
        Span<float> imagParts = stackalloc float[_fftBins];

        for (int t = 0; t < numFrames; t++)
        {
            // Extract real and imaginary parts
            for (int f = 0; f < _fftBins; f++)
            {
                var c = stft[t, f];
                realParts[f] = (float)c.Real;
                imagParts[f] = (float)c.Imaginary;
            }

            // Compute power = real^2 + imag^2 using SIMD
            var rowSpan = GetRowSpan(powerSpec, t);
            TensorPrimitives.Multiply(realParts, realParts, rowSpan);
            TensorPrimitives.MultiplyAdd(imagParts, imagParts, rowSpan, rowSpan);
        }

        return powerSpec;
    }

    private static Span<float> GetRowSpan(float[,] array, int row)
    {
        return System.Runtime.InteropServices.MemoryMarshal.CreateSpan(
            ref array[row, 0],
            array.GetLength(1));
    }

    private float[,] ApplyMelFilterbank(float[,] powerSpec)
    {
        var numFrames = powerSpec.GetLength(0);
        var melSpec = new float[numFrames, _options.MelBins];

        for (int t = 0; t < numFrames; t++)
        {
            var powerFrame = GetRowSpan(powerSpec, t);
            var melFrame = GetRowSpan(melSpec, t);

            for (int m = 0; m < _options.MelBins; m++)
            {
                var filterRow = GetRowSpan(_melFilterbank, m);
                // Use SIMD dot product for matrix multiplication
                melFrame[m] = TensorPrimitives.Dot(powerFrame, filterRow);
            }
        }

        return melSpec;
    }

    private void ApplyLogCompression(float[,] melSpec)
    {
        var numFrames = melSpec.GetLength(0);
        var constant = _options.LogCompressionConstant;

        Span<float> temp = stackalloc float[_options.MelBins];

        for (int t = 0; t < numFrames; t++)
        {
            var row = GetRowSpan(melSpec, t);
            
            // Use SIMD operations: log(1 + constant * x)
            TensorPrimitives.Multiply(row, constant, temp);
            TensorPrimitives.Add(temp, 1.0f, temp);
            TensorPrimitives.Log(temp, row);
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

        // Create FFT bin frequencies
        var fftFreqs = new float[_fftBins];
        for (int i = 0; i < _fftBins; i++)
        {
            fftFreqs[i] = i * _options.SampleRate / (float)_options.FrameSize;
        }

        // Convert min/max frequencies to mel scale
        var minMel = HzToMel(_options.MinFrequency);
        var maxMel = HzToMel(_options.MaxFrequency);

        // Create mel bin edges (linearly spaced in mel scale)
        var melFreqs = new float[_options.MelBins + 2];
        var melStep = (maxMel - minMel) / (_options.MelBins + 1);
        for (int i = 0; i < melFreqs.Length; i++)
        {
            melFreqs[i] = MelToHz(minMel + (i * melStep));
        }

        // Compute differences between adjacent mel frequencies
        var fdiff = new float[melFreqs.Length - 1];
        for (int i = 0; i < fdiff.Length; i++)
        {
            fdiff[i] = melFreqs[i + 1] - melFreqs[i];
        }

        // Create ramps array: outer subtraction of melFreqs and fftFreqs
        var ramps = new float[melFreqs.Length, _fftBins];
        for (int i = 0; i < melFreqs.Length; i++)
        {
            for (int j = 0; j < _fftBins; j++)
            {
                ramps[i, j] = melFreqs[i] - fftFreqs[j];
            }
        }

        // Build triangular filters
        Span<float> lower = stackalloc float[_fftBins];
        Span<float> upper = stackalloc float[_fftBins];

        for (int i = 0; i < _options.MelBins; i++)
        {
            var rampLower = GetRowSpan(ramps, i);
            var rampUpper = GetRowSpan(ramps, i + 2);
            var filterRow = GetRowSpan(filterbank, i);

            // Compute lower = -ramps[i] / fdiff[i]
            TensorPrimitives.Multiply(rampLower, -1.0f / fdiff[i], lower);
            
            // Compute upper = ramps[i+2] / fdiff[i+1]
            TensorPrimitives.Multiply(rampUpper, 1.0f / fdiff[i + 1], upper);

            // filterbank[i] = max(0, min(lower, upper))
            TensorPrimitives.Min(lower, upper, filterRow);
            TensorPrimitives.Max(filterRow, 0.0f, filterRow);

            // Apply Slaney normalization
            float enorm = 2.0f / (melFreqs[i + 2] - melFreqs[i]);
            TensorPrimitives.Multiply(filterRow, enorm, filterRow);
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
