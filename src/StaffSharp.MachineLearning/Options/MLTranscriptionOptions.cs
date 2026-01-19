namespace StaffSharp.MachineLearning.Options;

/// <summary>
/// Configuration options for polyphonic transcription.
/// </summary>
public sealed record MLTranscriptionOptions
{
    /// <summary>
    /// Path to the ONNX model file.
    /// </summary>
    public string? ModelPath { get; init; }

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
    /// Default: 0.05 seconds (200ms).
    /// </summary>
    public float MinNoteLengthSeconds { get; init; } = 0.05f;

    /// <summary>
    /// Maximum gap duration in seconds to tolerate before ending a note.
    /// Allows bridging brief signal dropouts.
    /// Default: 0.05 seconds (50ms).
    /// </summary>
    public float MinGapSeconds { get; init; } = 0.05f;

    /// <summary>
    /// Minimum velocity threshold for onset detection (0.0-1.0).
    /// Onsets with velocity below this are ignored (ghost note suppression).
    /// Default: 0.1
    /// </summary>
    public float MinVelocity { get; init; } = 0.1f;

    /// <summary>
    /// Minimum frame probability required for onset validation (0.0-1.0).
    /// Ensures frame and onset heads agree (consensus check).
    /// Default: 0.3
    /// </summary>
    public float MinFrameForOnset { get; init; } = 0.3f;

    /// <summary>
    /// Enable GPU acceleration if available.
    /// Requires CUDA and Microsoft.ML.OnnxRuntime.Gpu package.
    /// Default: false (CPU only).
    /// </summary>
    public bool UseGpu { get; init; }
}
