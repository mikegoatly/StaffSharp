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

    /*
     *   onset_thresh: 0.7805672483321652
  offset_thresh: 0.8494771227136267
  frame_thresh: 0.9495884991459451
  min_velocity: 0.045312027317955195
  min_duration: 0.031768928719288964
  gap_tolerance: 0.05738326419809668
  min_frame_for_onset: 0.12840639963156808
    */

    /// <summary>
    /// Threshold for onset detection (0.0-1.0).
    /// Higher values = fewer false positives, more missed notes.
    /// </summary>
    public float OnsetThreshold { get; init; } = 0.86057f;

    /// <summary>
    /// Threshold for offset detection (0.0-1.0).
    /// Higher values = stricter note release detection.
    /// </summary>
    public float OffsetThreshold { get; init; } = 0.52948f;

    /// <summary>
    /// Threshold for frame activation (0.0-1.0).
    /// Higher values = stricter note activation requirement.
    /// </summary>
    public float FrameThreshold { get; init; } = 0.94959f;

    /// <summary>
    /// Minimum note length in seconds.
    /// Notes shorter than this will be filtered out.
    /// </summary>
    public float MinNoteLengthSeconds { get; init; } = 0.05f;

    /// <summary>
    /// Maximum gap duration in seconds to tolerate before ending a note.
    /// Allows bridging brief signal dropouts.
    /// </summary>
    public float MinGapSeconds { get; init; } = 0.05738f;

    /// <summary>
    /// Minimum velocity threshold for onset detection (0.0-1.0).
    /// Onsets with velocity below this are ignored (ghost note suppression).
    /// </summary>
    public float MinVelocity { get; init; } = 0.045312f;

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

    /// <summary>
    /// Options for harmonic suppression to filter out overtones detected as separate notes.
    /// When set, notes starting within the configured temporal window with harmonic relationships
    /// (octaves, perfect 12ths, 2 octaves) are filtered to keep only the fundamental (lowest pitch).
    /// </summary>
    public HarmonicSuppressionOptions HarmonicSuppressionOptions { get; init; } = new(true);
}
