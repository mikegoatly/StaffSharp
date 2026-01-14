namespace StaffSharp.MachineLearning.Options;

/// <summary>
/// Configuration options for polyphonic transcription.
/// </summary>
public sealed record PolyphonicTranscriptionOptions
{
    /// <summary>
    /// Path to the ONNX model file.
    /// </summary>
    public string ModelPath { get; init; } = "models/onsets_frames.onnx";

    /// <summary>
    /// Feature extraction options.
    /// These parameters must match the model's training configuration.
    /// </summary>
    public MelSpectrogramOptions FeatureOptions { get; init; } = new();

    /// <summary>
    /// Threshold for onset detection (0.0-1.0).
    /// Higher values = fewer false positives, more missed notes.
    /// Default: 0.5
    /// </summary>
    public float OnsetThreshold { get; init; } = 0.5f;

    /// <summary>
    /// Threshold for offset detection (0.0-1.0).
    /// Higher values = stricter note release detection.
    /// Default: 0.5
    /// </summary>
    public float OffsetThreshold { get; init; } = 0.5f;

    /// <summary>
    /// Threshold for frame activation (0.0-1.0).
    /// Higher values = stricter note activation requirement.
    /// Default: 0.5
    /// </summary>
    public float FrameThreshold { get; init; } = 0.5f;

    /// <summary>
    /// Minimum note length in seconds.
    /// Notes shorter than this will be filtered out.
    /// Default: 0.05 seconds (50ms).
    /// </summary>
    public float MinNoteLengthSeconds { get; init; } = 0.05f;

    /// <summary>
    /// Enable GPU acceleration if available.
    /// Requires CUDA and Microsoft.ML.OnnxRuntime.Gpu package.
    /// Default: false (CPU only).
    /// </summary>
    public bool UseGpu { get; init; }
}
