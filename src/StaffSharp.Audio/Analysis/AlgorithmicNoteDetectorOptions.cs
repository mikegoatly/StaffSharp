using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Analysis.Tempo;

namespace StaffSharp.Audio.Analysis;

/// <summary>
/// Configuration options for <see cref="AlgorithmicNoteDetector"/>.
/// All options are nullable and will use sensible defaults if not specified.
/// </summary>
public sealed record AlgorithmicNoteDetectorOptions
{
    /// <summary>
    /// Options for onset detection. If null, uses default <see cref="Onset.SpectralFluxOnsetDetector"/>.
    /// </summary>
    public OnsetDetectionOptions? OnsetOptions { get; init; }

    /// <summary>
    /// Options for pitch detection. If null, uses default <see cref="Pitch.PyinPitchDetector"/>.
    /// </summary>
    public PitchDetectionOptions? PitchOptions { get; init; }

    /// <summary>
    /// Options for tempo detection. If null, uses default <see cref="CombFilterTempoDetector"/>.
    /// </summary>
    public TempoDetectionOptions? TempoOptions { get; init; }

    /// <summary>
    /// Options for boundary detection. If null, uses default <see cref="Boundaries.EnergyBasedBoundaryDetector"/>.
    /// </summary>
    public BoundaryDetectionOptions? BoundaryOptions { get; init; }

    // Note: TimeSignatureDetection and Quantization don't currently have options classes,
    // so they'll always use defaults (SimpleTimeSignatureDetector, SimpleMonophonicQuantizer)
}
