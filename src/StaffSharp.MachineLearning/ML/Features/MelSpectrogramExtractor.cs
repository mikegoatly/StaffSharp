namespace StaffSharp.MachineLearning.ML.Features;

using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;

using MathNet.Numerics.IntegralTransforms;

using StaffSharp.Audio;
using StaffSharp.Audio.Numerics;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.Options;

/// <summary>
/// Extracts mel spectrogram features from audio.
/// This implementation is used in both data preparation and model inference for consistency.
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

        // Convert to mono first
        var processedAudio = audio.ToMono();

        // Detect content boundaries
        var (contentStart, contentEnd) = processedAudio.DetectContent();
        progress.EmitDiagnostics("ContentStart", contentStart);
        progress.EmitDiagnostics("ContentEnd", contentEnd);
        progress.EmitDiagnostics("ContentDuration", contentEnd - contentStart);

        // Mute silence outside content range
        processedAudio = processedAudio.MuteOutsideRange(contentStart, contentEnd);

        // Normalize entire audio using RMS
        // This provides more consistent loudness than peak normalization
        (processedAudio, var rmsStats) = processedAudio.NormalizeRms();
        progress.EmitDiagnostics("RmsNormalizationStats", rmsStats);
        progress.EmitDiagnostics("NormalizedWaveform", processedAudio.Samples.ToArray());
        progress.EmitDiagnostics("NormalizedSampleRate", processedAudio.SampleRate);

        // Resample audio to target sample rate if needed
        var resampledAudio = processedAudio.Resample(_options.SampleRate);

        if (resampledAudio != processedAudio)
        {
            progress.EmitDiagnostics("ResampledRate", resampledAudio.SampleRate);
            progress.EmitDiagnostics("ResampledWaveform", resampledAudio.Samples.ToArray());
        }

        // --- Feature Extraction ---
        var melSpec = ComputeMelSpectrogramStreaming(resampledAudio);

        progress.EmitDiagnostics("MelSpectrogram", melSpec);

        return melSpec;
    }

    private float[,] ComputeMelSpectrogramStreaming(AudioBuffer audio)
    {
        var samples = audio.Samples.Span;

        // We pad with zeros so that Frame 0 is centered at Time 0.
        // Without this, features are shifted by (FrameSize/2) / SampleRate seconds (approx 64ms).
        int padLength = _options.FrameSize / 2;
        int totalPaddedLength = samples.Length + (padLength * 2);
        int numFrames = ((totalPaddedLength - _options.FrameSize) / _options.HopSize) + 1;

        if (numFrames <= 0)
        {
            throw new ArgumentException("Audio is too short", nameof(audio));
        }

        var melSpec = new float[numFrames, _options.MelBins];

        // Allocate Reusable Buffers
        // MathNet Fourier wants Complex[], so we use that as our workspace
        var fftBuffer = new Complex[_options.FrameSize];
        var powerFrame = new float[_fftBins];

        // Pre-calculate constants
        float scale = MathF.Sqrt(_options.FrameSize);
        float logConstant = _options.LogCompressionConstant;
        int hopSize = _options.HopSize;

        // 4. Process Frame-by-Frame (Streaming)
        for (int frameIdx = 0; frameIdx < numFrames; frameIdx++)
        {
            // Windowing & FFT Preparation
            // Calculate where this frame starts in the *padded* timeline
            int paddedFrameStart = frameIdx * hopSize;

            // Calculate where this corresponds to in the *actual* samples
            int actualSampleStart = paddedFrameStart - padLength;

            // Fill fftBuffer with Windowed Samples
            CopyAndWindowFrame(samples, fftBuffer, actualSampleStart);

            // FFT
            Fourier.Forward(fftBuffer, FourierOptions.Default);

            // Compute Power Spectrum
            // Power = (Real^2 + Imag^2) * scale
            ComputePowerFrame(fftBuffer, powerFrame, scale);

            // Apply Mel Filterbank & Log Compression immediately
            // We write directly into the final 'melSpec' array
            ApplyMelAndLog(powerFrame, melSpec, frameIdx, logConstant);
        }

        return melSpec;
    }

    /// <summary>
    /// Copies samples from source to fftBuffer, applying the window function.
    /// Handles the "Virtual Padding" (reading zeros if out of bounds) without allocating a padded array.
    /// </summary>
    private void CopyAndWindowFrame(ReadOnlySpan<float> sourceSamples, Complex[] fftBuffer, int startSampleIndex)
    {
        int frameSize = _options.FrameSize;

        // Fast Path: If the entire frame is within the bounds of the source array
        if (startSampleIndex >= 0 && startSampleIndex + frameSize <= sourceSamples.Length)
        {
            var slice = sourceSamples.Slice(startSampleIndex, frameSize);
            for (int i = 0; i < frameSize; i++)
            {
                // Real = Sample * Window, Imag = 0
                fftBuffer[i] = new Complex(slice[i] * _window[i], 0);
            }
        }
        else
        {
            // Slow/Safe Path: Edge cases (start or end of file)
            for (int i = 0; i < frameSize; i++)
            {
                int sampleIdx = startSampleIndex + i;
                float sample = 0f;

                // Virtual Zero Padding
                if (sampleIdx >= 0 && sampleIdx < sourceSamples.Length)
                {
                    sample = sourceSamples[sampleIdx];
                }

                fftBuffer[i] = new Complex(sample * _window[i], 0);
            }
        }
    }

    private static void ComputePowerFrame(Complex[] fftBuffer, float[] powerFrame, float scale)
    {
        // We only need the first _fftBins (Nyquist), ignoring the symmetric half
        for (int i = 0; i < powerFrame.Length; i++)
        {
            var c = fftBuffer[i];

            // |c|^2 = Real^2 + Imag^2
            double magSquared = (c.Real * c.Real) + (c.Imaginary * c.Imaginary);
            powerFrame[i] = (float)magSquared * (scale * scale);
        }
    }

    private void ApplyMelAndLog(float[] powerFrame, float[,] melSpec, int frameIdx, float logConstant)
    {
        int melBins = _options.MelBins;

        // Get the output row for this frame
        var melRow = GetRowSpan(melSpec, frameIdx);
        var powerSpan = new ReadOnlySpan<float>(powerFrame);

        for (int m = 0; m < melBins; m++)
        {
            var filterRow = GetRowSpan(_melFilterbank, m);

            // Mel Dot Product then log compression
            float melValue = TensorPrimitives.Dot(powerSpan, filterRow);
            melRow[m] = MathF.Log(1.0f + (logConstant * melValue));
        }
    }

    private static Span<float> GetRowSpan(float[,] array, int row)
    {
        return MemoryMarshal.CreateSpan(ref array[row, 0], array.GetLength(1));
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
        float fftFreqMult = (float)_options.SampleRate / _options.FrameSize;
        for (int i = 0; i < _fftBins; i++)
        {
            fftFreqs[i] = i * fftFreqMult;
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