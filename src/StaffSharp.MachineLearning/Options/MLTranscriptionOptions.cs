using StaffSharp.Audio.Analysis.Tempo;

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
    /// Options for tempo detection. If null, uses default <see cref="CombFilterTempoDetector"/>.
    /// </summary>
    public TempoDetectionOptions? TempoOptions { get; init; }

    /// <summary>
    /// Feature extraction options.
    /// These parameters must match the model's training configuration.
    /// </summary>
    public MelSpectrogramOptions FeatureOptions { get; init; } = new();

    /// <summary>
    /// Threshold for onset detection (0.0-1.0).
    /// Higher values = fewer false positives, more missed notes.
    /// </summary>
    public float OnsetThreshold { get; init; } = 0.7649203112821892f;

    /// <summary>
    /// Threshold for offset detection (0.0-1.0).
    /// Higher values = stricter note release detection.
    /// </summary>
    public float OffsetThreshold { get; init; } = 0.698630524739962f;

    /// <summary>
    /// Threshold for frame activation (0.0-1.0).
    /// Higher values = stricter note activation requirement.
    /// </summary>
    public float FrameThreshold { get; init; } = 0.155380169870799f;

    /// <summary>
    /// Minimum note length in seconds.
    /// Notes shorter than this will be filtered out.
    /// </summary>
    public float MinNoteLengthSeconds { get; init; } = 0.053654719841124285f;

    /// <summary>
    /// Maximum gap duration in seconds to tolerate before ending a note.
    /// Allows bridging brief signal dropouts.
    /// </summary>
    public float MinGapSeconds { get; init; } = 0.037595454973831766f;

    /// <summary>
    /// Minimum velocity threshold for onset detection (0.0-1.0).
    /// Onsets with velocity below this are ignored (ghost note suppression).
    /// </summary>
    public float MinVelocity { get; init; } = 0.1884614567666589f;

    /// <summary>
    /// Minimum frame probability required for onset validation (0.0-1.0).
    /// Ensures frame and onset heads agree (consensus check).
    /// </summary>
    public float MinFrameForOnset { get; init; } = 0.12060303762483016f;

    /// <summary>
    /// Enable GPU acceleration if available.
    /// Requires CUDA and Microsoft.ML.OnnxRuntime.Gpu package.
    /// Default: false (CPU only).
    /// </summary>
    public bool UseGpu { get; init; }

    /// <summary>
    /// If true, overlapping notes are grouped into chords sharing a single stem/voice.
    /// If false, overlapping notes may be split into separate rhythmic voices.
    /// <para>
    /// Recommended: <c>true</c> for simple piano/lead sheets, <c>false</c> for orchestral scores.
    /// </para>
    /// </summary>
    public bool TreatPolyphonyAsChords { get; init; } = true;
}
