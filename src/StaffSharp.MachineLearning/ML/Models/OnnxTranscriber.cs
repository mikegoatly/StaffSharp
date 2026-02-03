namespace StaffSharp.MachineLearning.ML.Models;

using System;
using System.Numerics.Tensors;
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
internal sealed class OnnxTranscriber : IMLTranscriber, IDisposable
{
    private const int PianoKeyCount = 88; // MIDI notes 21-108 (A0-C8)

    private readonly InferenceSession _session;
    private readonly MelSpectrogramExtractor _featureExtractor;
    private readonly MLTranscriptionOptions _options;
    private bool? _requiresSigmoid;
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
            _session = CreateInferenceSession(sessionOptions);
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

        // 1. Extract features
        var features = _featureExtractor.ExtractFeatures(progress, audio);

        int totalFrames = features.GetLength(0);
        int melBins = features.GetLength(1);

        // 2. Configuration
        // The Margin is how much context the BiLSTM needs on each side to be accurate.
        const int Margin = 100;
        // The Stride is how many *new* frames of valid output we generate per step.
        const int Stride = 1800;
        // The Window is the actual chunk size sent to ONNX (Stride + Context on both sides).
        // Note: At the start/end of the file, the actual input will be smaller than this.
        const int MaxWindowSize = Stride + (2 * Margin);

        // 3. Allocate a single buffer large enough for the biggest possible window
        float[] inferenceBuffer = new float[1 * MaxWindowSize * melBins];

        // 4. Prepare output accumulators
        var allOnsets = new float[totalFrames, PianoKeyCount];
        var allOffsets = new float[totalFrames, PianoKeyCount];
        var allFrames = new float[totalFrames, PianoKeyCount];
        var allVelocities = new float[totalFrames, PianoKeyCount];

        // 5. Iterate over the TARGET output
        // We step through the *output* timeline.
        for (int writeStart = 0; writeStart < totalFrames; writeStart += Stride)
        {
            // Calculate how many frames we need to write in this block
            // (Usually equals Stride, unless we are at the very end of the song)
            int writeLength = Math.Min(Stride, totalFrames - writeStart);

            // Determine the Input Window needed to generate this Output Block
            // We try to grab 'Margin' frames before and after the write block
            int inputStart = Math.Max(0, writeStart - Margin);
            int inputEnd = Math.Min(totalFrames, writeStart + writeLength + Margin);
            int inputLength = inputEnd - inputStart;

            // Extract the slice (Input)
            CopyFeaturesToBuffer(features, inferenceBuffer, inputStart, inputLength);

            // Create OrtValue directly from reused buffer with dynamic shape (batch=1, time=inputLength, mel_bins)
            // Note: We pass the entire buffer, but the shape tells ONNX to only use the first inputLength*melBins elements
            var inputShape = new long[] { 1, inputLength, melBins };
            using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(inferenceBuffer, inputShape);

            // Run Inference
            var (onsets, offsets, frames, vels) = await RunInferenceAsync(inputOrtValue).ConfigureAwait(false);

            // Calculate "Where is my valid data inside this prediction?"
            // If we are at the start of the file (writeStart == 0), inputStart is 0, so valid data starts at 0.
            // Otherwise, we added Margin frames to the left, so valid data starts at Margin.
            int readOffset = (writeStart == 0) ? 0 : Margin;

            // Copy the valid middle section to the final output
            CopySlice(onsets, allOnsets, readOffset, writeStart, writeLength);
            CopySlice(offsets, allOffsets, readOffset, writeStart, writeLength);
            CopySlice(frames, allFrames, readOffset, writeStart, writeLength);
            CopySlice(vels, allVelocities, readOffset, writeStart, writeLength);

            progress.ReportProgress($"Transcribing: {Math.Min(100, (int)(100.0 * writeStart / totalFrames))}%");
        }

        // Emit diagnostics for the complete accumulated probability matrices
        progress.EmitDiagnostics("OnsetProbabilities", allOnsets);
        progress.EmitDiagnostics("FrameProbabilities", allFrames);
        progress.EmitDiagnostics("OffsetProbabilities", allOffsets);

        var frameRate = _options.FeatureOptions.SampleRate / _options.FeatureOptions.HopSize;

        return new PolyphonicTranscriptionResult(
            allFrames,
            allOnsets,
            allOffsets,
            allVelocities,
            frameRate,
            _options.FeatureOptions.SampleRate);
    }

    // Copy a slice of features into the inference buffer
    private static void CopyFeaturesToBuffer(float[,] features, float[] buffer, int startFrame, int length)
    {
        var melBins = features.GetLength(1);

        // Create a span over the relevant section of the 2D array
        var sourceSpan = MemoryMarshal.CreateReadOnlySpan(
            ref features[startFrame, 0],
            length * melBins);

        sourceSpan.CopyTo(buffer);
    }

    // Helper to copy from 3D batch output [1, Time, 88] to 2D accumulators [TotalTime, 88]
    private static void CopySlice(float[,,] sourceBatch, float[,] dest, int sourceTimeStart, int destTimeStart, int length)
    {
        // Span-based copy for speed
        // Source: sourceBatch[0, sourceTimeStart, 0]
        // Dest: dest[destTimeStart, 0]

        var sourceSpan = MemoryMarshal.CreateReadOnlySpan(
            ref sourceBatch[0, sourceTimeStart, 0],
            length * PianoKeyCount);

        var destSpan = MemoryMarshal.CreateSpan(
            ref dest[destTimeStart, 0],
            length * PianoKeyCount);

        sourceSpan.CopyTo(destSpan);
    }

    private async Task<(float[,,] onsets, float[,,] offsets, float[,,] frames, float[,,] velocities)> RunInferenceAsync(OrtValue inputOrtValue)
    {
        // Get input name (typically "input" or "mel_spectrogram")
        var inputName = _session.InputNames[0];

        // Pre-allocate output OrtValues with expected shapes
        // Shape: (batch=1, time, keys=88)
        var inputShape = inputOrtValue.GetTensorTypeAndShape().Shape;
        var batch = inputShape[0]; // Should be 1
        var timeFrames = inputShape[1];
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
        _requiresSigmoid ??= RequiresSigmoid(onsets);
        if (_requiresSigmoid.GetValueOrDefault())
        {
            ApplySigmoidInPlace(onsets);
            ApplySigmoidInPlace(offsets);
            ApplySigmoidInPlace(frames);
            // Note: velocities might already be in [0,1] range even if others are logits
            if (RequiresSigmoid(velocities))
            {
                ApplySigmoidInPlace(velocities);
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

    /// <summary>
    /// Detects if the tensor contains logits (values outside [0,1]) that require sigmoid activation.
    /// Samples a subset of values for efficiency.
    /// </summary>
    private static bool RequiresSigmoid(float[,,] tensor)
    {
        var totalLength = tensor.Length;
        if (totalLength == 0)
        {
            return false;
        }

        // Sample up to 100 values across the tensor
        var sampleSize = Math.Min(100, totalLength);
        var step = Math.Max(1, totalLength / sampleSize);

        var span = MemoryMarshal.CreateReadOnlySpan(ref tensor[0, 0, 0], totalLength);

        for (int idx = 0; idx < totalLength; idx += step)
        {
            var value = span[idx];
            // If we find any value outside [0,1] range, it's likely logits
            if (value < 0.0f || value > 1.0f)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies sigmoid activation to convert logits to probabilities.
    /// </summary>
    private static void ApplySigmoidInPlace(float[,,] tensor)
    {
        var totalLength = tensor.Length;

        var span = MemoryMarshal.CreateSpan(ref tensor[0, 0, 0], totalLength);

        TensorPrimitives.Sigmoid(span, span);
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

    private InferenceSession CreateInferenceSession(SessionOptions sessionOptions)
    {
        if (!string.IsNullOrWhiteSpace(_options.ModelPath))
        {
            if (!File.Exists(_options.ModelPath))
            {
                throw new FileNotFoundException($"ONNX model not found at: {_options.ModelPath}", _options.ModelPath);
            }

            return new InferenceSession(_options.ModelPath, sessionOptions);
        }

        return new InferenceSession(Path.Combine(AppContext.BaseDirectory, "Models", "model_v3_dynamic.onnx"), sessionOptions);
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
