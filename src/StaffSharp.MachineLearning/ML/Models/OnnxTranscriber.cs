namespace StaffSharp.MachineLearning.ML.Models;

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using StaffSharp.Audio;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.ML.Features;
using StaffSharp.MachineLearning.Options;

/// <summary>
/// ONNX-based polyphonic music transcription using the "Onsets and Frames" model.
/// </summary>
/// <remarks>
/// This transcriber loads a pre-trained ONNX model and performs inference on audio.
/// The model should output three tensors:
/// - "onset_probs": (batch, time, 88) - onset probabilities
/// - "frame_probs": (batch, time, 88) - frame activation probabilities
/// - "velocities": (batch, time, 88) - normalized velocities [0-1]
///
/// The model expects input of shape (batch, time, mel_bins) where mel_bins is typically 229.
/// </remarks>
public sealed class OnnxTranscriber : IMLTranscriber, IDisposable
{
    private const int PianoKeyCount = 88; // MIDI notes 21-108 (A0-C8)

    private readonly InferenceSession _session;
    private readonly MelSpectrogramExtractor _featureExtractor;
    private readonly MLTranscriptionOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnnxTranscriber"/> class
    /// using the model path from options.
    /// </summary>
    /// <param name="options">Transcription options including model path.</param>
    public OnnxTranscriber(MLTranscriptionOptions? options = null)
    {
        _options = options ?? new MLTranscriptionOptions();
        _featureExtractor = new MelSpectrogramExtractor(_options.FeatureOptions);

        // Create and configure session options
#pragma warning disable CA2000 // Dispose objects before losing scope - owned by _session
        var sessionOptions = new SessionOptions();
#pragma warning restore CA2000 // Dispose objects before losing scope
        try
        {
            // Configure execution providers (GPU or CPU)
            if (_options.UseGpu)
            {
                // Try to use CUDA if available - may throw if CUDA is not available
                sessionOptions.AppendExecutionProvider_CUDA();
            }

            // Load the model
            _session = new InferenceSession(LoadModel(), sessionOptions);
        }
        catch
        {
            // Clean up session options if session creation fails
            sessionOptions.Dispose();
            throw;
        }

        // Validate model inputs/outputs
        ValidateModelSignature();
    }

    /// <inheritdoc/>
    public async Task<PolyphonicTranscriptionResult> TranscribeAsync(PipelineProgress progress, AudioBuffer audio)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(audio);

        // 1. Extract mel spectrogram features
        var features = _featureExtractor.ExtractFeatures(progress, audio);

        // 2. Convert features to ONNX tensor (batch_size=1, time, mel_bins)
        var inputTensor = ConvertToTensor(features);

        // 3. Run inference
        var (onsetProbs, offsetProbs, frameProbs, velocities) = await RunInferenceAsync(inputTensor).ConfigureAwait(false);

        // 4. Validate output shapes
        ValidateOutputShapes(onsetProbs, offsetProbs, frameProbs, velocities);

        // 5. Convert from (batch, time, keys) to (time, keys)
        var onsetRoll = ExtractBatch(onsetProbs);
        var offsetRoll = ExtractBatch(offsetProbs);
        var pianoRoll = ExtractBatch(frameProbs);
        var velocityRoll = ExtractBatch(velocities);

        progress.EmitDiagnostics("OnsetProbabilities", onsetRoll);
        progress.EmitDiagnostics("FrameProbabilities", pianoRoll);
        progress.EmitDiagnostics("OffsetProbabilities", offsetRoll);

        // 6. Compute frame rate
        var frameRate = _options.FeatureOptions.SampleRate / _options.FeatureOptions.HopSize;

        return new PolyphonicTranscriptionResult(
            pianoRoll,
            onsetRoll,
            offsetRoll,
            velocityRoll,
            frameRate,
            _options.FeatureOptions.SampleRate);
    }

    private static DenseTensor<float> ConvertToTensor(float[,] features)
    {
        var numFrames = features.GetLength(0);
        var numMelBins = features.GetLength(1);

        // Create tensor with shape (batch=1, time, mel_bins)
        var tensor = new DenseTensor<float>([1, numFrames, numMelBins]);

        // Get the underlying buffer as a span
        var tensorSpan = tensor.Buffer.Span;
        var sourceSpan = MemoryMarshal.CreateReadOnlySpan(
            ref features[0, 0],
            numFrames * numMelBins);

        // Single bulk copy operation
        sourceSpan.CopyTo(tensorSpan);

        return tensor;
    }

    private async Task<(float[,,] onsets, float[,,] offsets, float[,,] frames, float[,,] velocities)> RunInferenceAsync(DenseTensor<float> inputTensor)
    {
        // Get input name (typically "input" or "mel_spectrogram")
        var inputName = _session.InputNames[0];

        // Create OrtValue from tensor
        using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(
            inputTensor.Buffer.ToArray(),
            [.. inputTensor.Dimensions.ToArray().Select(d => (long)d)]);

        // Pre-allocate output OrtValues with expected shapes
        // Shape: (batch=1, time, keys=88)
        var batch = inputTensor.Dimensions[0]; // Should be 1
        var timeFrames = inputTensor.Dimensions[1];
        var outputShape = new long[] { batch, timeFrames, PianoKeyCount };

        using var onsetOutput = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, outputShape);
        using var offsetOutput = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, outputShape);
        using var frameOutput = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, outputShape);
        using var velocityOutput = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, outputShape);

        // Define output names and pre-allocated values
        var outputNames = new[] { "onset_probs", "offset_probs", "frame_probs", "velocities" };
        var outputValues = new[] { onsetOutput, offsetOutput, frameOutput, velocityOutput };

        // Run inference with RunAsync
        using var runOptions = new RunOptions();
        await _session.RunAsync(
            runOptions,
            [inputName],
            [inputOrtValue],
            outputNames,
            outputValues).ConfigureAwait(false);

        // Extract outputs from the pre-allocated OrtValues
        var onsets = ExtractOrtValueOutput(onsetOutput, "onset_probs");
        var offsets = ExtractOrtValueOutput(offsetOutput, "offset_probs");
        var frames = ExtractOrtValueOutput(frameOutput, "frame_probs");
        var velocities = ExtractOrtValueOutput(velocityOutput, "velocities");

        // Auto-detect if outputs are logits (need sigmoid) or probabilities
        // Check if values are outside [0,1] range, indicating logits
        if (RequiresSigmoid(onsets))
        {
            onsets = ApplySigmoid3D(onsets);
            offsets = ApplySigmoid3D(offsets);
            frames = ApplySigmoid3D(frames);
            // Note: velocities might already be in [0,1] range even if others are logits
            if (RequiresSigmoid(velocities))
            {
                velocities = ApplySigmoid3D(velocities);
            }
        }

        return (onsets, offsets, frames, velocities);
    }

    private static float[,,] ExtractOrtValueOutput(OrtValue ortValue, string name)
    {
        var tensor = ortValue.GetTensorDataAsSpan<float>();
        var dimensions = ortValue.GetTensorTypeAndShape().Shape;

        if (dimensions.Length != 3)
        {
            throw new InvalidOperationException(
                $"Expected 3D tensor for output '{name}', got {dimensions.Length}D tensor");
        }

        // Convert to 3D array (batch, time, keys)
        var batch = (int)dimensions[0];
        var time = (int)dimensions[1];
        var keys = (int)dimensions[2];

        var array = new float[batch, time, keys];

        // Use span-based bulk copy for maximum performance
        var totalElements = batch * time * keys;
        var arraySpan = MemoryMarshal.CreateSpan(ref array[0, 0, 0], totalElements);
        tensor.CopyTo(arraySpan);

        return array;
    }

    private static float[,] ExtractBatch(float[,,] tensor)
    {
        // Extract first batch (we always use batch_size=1)
        var time = tensor.GetLength(1);
        var keys = tensor.GetLength(2);

        var result = new float[time, keys];

        // Use span-based bulk copy for better performance
        var totalElements = time * keys;
        var sourceSpan = MemoryMarshal.CreateReadOnlySpan(ref tensor[0, 0, 0], totalElements);
        var destSpan = MemoryMarshal.CreateSpan(ref result[0, 0], totalElements);
        sourceSpan.CopyTo(destSpan);

        return result;
    }

    /// <summary>
    /// Detects if the tensor contains logits (values outside [0,1]) that require sigmoid activation.
    /// Samples a subset of values for efficiency.
    /// </summary>
    private static bool RequiresSigmoid(float[,,] tensor)
    {
        var batch = tensor.GetLength(0);
        var time = tensor.GetLength(1);
        var keys = tensor.GetLength(2);

        // Sample up to 100 values across the tensor
        var sampleSize = Math.Min(100, batch * time * keys);
        var step = Math.Max(1, (batch * time * keys) / sampleSize);

        var count = 0;
        for (int i = 0; i < batch && count < sampleSize; i++)
        {
            for (int j = 0; j < time && count < sampleSize; j += step)
            {
                for (int k = 0; k < keys && count < sampleSize; k++)
                {
                    var value = tensor[i, j, k];
                    // If we find any value outside [0,1] range, it's likely logits
                    if (value < 0.0f || value > 1.0f)
                    {
                        return true;
                    }
                    count++;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Applies sigmoid activation to convert logits to probabilities.
    /// </summary>
    private static float[,,] ApplySigmoid3D(float[,,] tensor)
    {
        var batch = tensor.GetLength(0);
        var time = tensor.GetLength(1);
        var keys = tensor.GetLength(2);

        var result = new float[batch, time, keys];

        for (int i = 0; i < batch; i++)
        {
            for (int j = 0; j < time; j++)
            {
                for (int k = 0; k < keys; k++)
                {
                    result[i, j, k] = 1.0f / (1.0f + MathF.Exp(-tensor[i, j, k]));
                }
            }
        }

        return result;
    }

    private void ValidateModelSignature()
    {
        // Validate inputs
        if (_session.InputNames.Count == 0)
        {
            throw new InvalidOperationException("Model has no inputs");
        }

        // Validate outputs
        var expectedOutputs = new[] { "onset_probs", "offset_probs", "frame_probs", "velocities" };
        var actualOutputs = _session.OutputNames.ToHashSet();

        if (expectedOutputs.Any(e => !actualOutputs.Contains(e)))
        {
            throw new InvalidOperationException(
                $"Model outputs do not match expected outputs. " +
                $"Expected: {string.Join(", ", expectedOutputs)}; " +
                $"Actual: {string.Join(", ", actualOutputs)}");
        }
    }

    private static void ValidateOutputShapes(
        float[,,] onsets,
        float[,,] offsets,
        float[,,] frames,
        float[,,] velocities)
    {
        // Check batch size
        if (onsets.GetLength(0) != 1 || offsets.GetLength(0) != 1 || frames.GetLength(0) != 1 || velocities.GetLength(0) != 1)
        {
            throw new InvalidOperationException("Expected batch size of 1");
        }

        // Check time dimension
        var onsetTime = onsets.GetLength(1);
        var offsetTime = offsets.GetLength(1);
        var frameTime = frames.GetLength(1);
        var velocityTime = velocities.GetLength(1);

        if (onsetTime != offsetTime || onsetTime != frameTime || onsetTime != velocityTime)
        {
            throw new InvalidOperationException(
                $"Time dimensions mismatch: onsets={onsetTime}, offsets={offsetTime}, frames={frameTime}, velocities={velocityTime}");
        }

        // Note: The model may output slightly different number of frames due to padding/convolution
        // We allow some tolerance here

        // Check key dimension (should be 88 piano keys)
        var onsetKeys = onsets.GetLength(2);
        var offsetKeys = offsets.GetLength(2);
        var frameKeys = frames.GetLength(2);
        var velocityKeys = velocities.GetLength(2);

        if (onsetKeys != PianoKeyCount || offsetKeys != PianoKeyCount || frameKeys != PianoKeyCount || velocityKeys != PianoKeyCount)
        {
            throw new InvalidOperationException(
                $"Expected {PianoKeyCount} piano keys, got onsets={onsetKeys}, offsets={offsetKeys}, frames={frameKeys}, velocities={velocityKeys}");
        }
    }

    private byte[] LoadModel()
    {
        // Use the provided model path, or fallback to Models/model_dynamic.zip in the output directory
        var modelPath = string.IsNullOrWhiteSpace(_options.ModelPath)
            ? Path.Combine(AppContext.BaseDirectory, "Models", "model_dynamic.zip")
            : _options.ModelPath;
        return ModelLoader.LoadModel(modelPath);
    }

    /// <summary>
    /// Disposes the ONNX inference session.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
