namespace StaffSharp.MachineLearning.ML.Models;

using System;
using System.Runtime.InteropServices;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using StaffSharp;
using StaffSharp.Audio;
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
public sealed class OnnxPolyphonicTranscriber : IPolyphonicTranscriber, IDisposable
{
    private const int PianoKeyCount = 88; // MIDI notes 21-108 (A0-C8)

    private readonly InferenceSession _session;
    private readonly MelSpectrogramExtractor _featureExtractor;
    private readonly PolyphonicTranscriptionOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnnxPolyphonicTranscriber"/> class.
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file.</param>
    /// <param name="options">Transcription options.</param>
    public OnnxPolyphonicTranscriber(string modelPath, PolyphonicTranscriptionOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"ONNX model not found at: {modelPath}", modelPath);
        }

        _options = options ?? new PolyphonicTranscriptionOptions();
        _featureExtractor = new MelSpectrogramExtractor(_options.FeatureOptions);

        // Create and configure session options
        var sessionOptions = new SessionOptions();
        try
        {
            // Configure execution providers (GPU or CPU)
            if (_options.UseGpu)
            {
                // Try to use CUDA if available - may throw if CUDA is not available
                sessionOptions.AppendExecutionProvider_CUDA();
            }

            // Load the model
            _session = new InferenceSession(modelPath, sessionOptions);
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

    /// <summary>
    /// Initializes a new instance of the <see cref="OnnxPolyphonicTranscriber"/> class
    /// using the model path from options.
    /// </summary>
    /// <param name="options">Transcription options including model path.</param>
    public OnnxPolyphonicTranscriber(PolyphonicTranscriptionOptions options)
        : this(options?.ModelPath ?? throw new ArgumentNullException(nameof(options)), options)
    {
    }

    /// <inheritdoc/>
    public PolyphonicTranscriptionResult Transcribe(AudioBuffer audio)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(audio);

        // 1. Extract mel spectrogram features
        var features = _featureExtractor.ExtractFeatures(audio);
        var numFrames = features.GetLength(0);
        var numMelBins = features.GetLength(1);

        // 2. Convert features to ONNX tensor (batch_size=1, time, mel_bins)
        var inputTensor = ConvertToTensor(features);

        // 3. Run inference
        var (onsetProbs, offsetProbs, frameProbs, velocities) = RunInference(inputTensor);

        // 4. Validate output shapes
        ValidateOutputShapes(onsetProbs, offsetProbs, frameProbs, velocities, numFrames);

        // 5. Convert from (batch, time, keys) to (time, keys)
        var onsetRoll = ExtractBatch(onsetProbs);
        var offsetRoll = ExtractBatch(offsetProbs);
        var pianoRoll = ExtractBatch(frameProbs);
        var velocityRoll = ExtractBatch(velocities);

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
        var tensor = new DenseTensor<float>(new[] { 1, numFrames, numMelBins });
        
        // Get the underlying buffer as a span
        var tensorSpan = tensor.Buffer.Span;
        var sourceSpan = MemoryMarshal.CreateReadOnlySpan(
            ref features[0, 0], 
            numFrames * numMelBins);
        
        // Single bulk copy operation
        sourceSpan.CopyTo(tensorSpan);
        
        return tensor;
    }

    private (float[,,] onsets, float[,,] offsets, float[,,] frames, float[,,] velocities) RunInference(DenseTensor<float> inputTensor)
    {
        // Get input name (typically "input" or "mel_spectrogram")
        var inputName = _session.InputNames[0];

        // Create input container
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);

        // Extract outputs (expected names: onset_probs, offset_probs, frame_probs, velocities)
        var outputDict = results.ToDictionary(r => r.Name, r => (DisposableNamedOnnxValue)r);

        var onsets = ExtractOutput(outputDict, "onset_probs");
        var offsets = ExtractOutput(outputDict, "offset_probs");
        var frames = ExtractOutput(outputDict, "frame_probs");
        var velocities = ExtractOutput(outputDict, "velocities");

        return (onsets, offsets, frames, velocities);
    }

    private static float[,,] ExtractOutput(Dictionary<string, DisposableNamedOnnxValue> outputs, string name)
    {
        if (!outputs.TryGetValue(name, out var output))
        {
            throw new InvalidOperationException(
                $"Model output '{name}' not found. Available outputs: {string.Join(", ", outputs.Keys)}");
        }

        var tensor = output.AsTensor<float>();
        var dimensions = tensor.Dimensions.ToArray();

        if (dimensions.Length != 3)
        {
            throw new InvalidOperationException(
                $"Expected 3D tensor for output '{name}', got {dimensions.Length}D tensor");
        }

        // Convert to 3D array (batch, time, keys)
        var batch = dimensions[0];
        var time = dimensions[1];
        var keys = dimensions[2];

        var array = new float[batch, time, keys];

        // ONNX Runtime always returns DenseTensor for standard neural network inference
        // Use span-based bulk copy for maximum performance
        var totalElements = batch * time * keys;
        var denseTensor = (DenseTensor<float>)tensor;
        var tensorSpan = denseTensor.Buffer.Span;
        var arraySpan = MemoryMarshal.CreateSpan(ref array[0, 0, 0], totalElements);
        tensorSpan.CopyTo(arraySpan);

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

        foreach (var expected in expectedOutputs)
        {
            if (!actualOutputs.Contains(expected))
            {
                throw new InvalidOperationException(
                    $"Model missing expected output '{expected}'. " +
                    $"Available outputs: {string.Join(", ", actualOutputs)}");
            }
        }
    }

    private static void ValidateOutputShapes(
        float[,,] onsets,
        float[,,] offsets,
        float[,,] frames,
        float[,,] velocities,
        int expectedTimeFrames)
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

    /// <summary>
    /// Disposes the ONNX inference session.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _session?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
