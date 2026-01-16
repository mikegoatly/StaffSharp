using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Diagnostics;
using StaffSharp.Quantization;

namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Configuration options for the audio-to-score pipeline.
/// </summary>
public sealed record AudioPipelineOptions
{
    private INoteDetector? _noteDetector;

    /// <summary>
    /// Gets or sets the note detector for transcribing audio to note events.
    /// Defaults to AlgorithmicNoteDetector with standard settings.
    /// </summary>
    public INoteDetector NoteDetector
    {
        get
        {
            _noteDetector ??= CreateDefaultNoteDetector();
            return _noteDetector;
        }
        set => _noteDetector = value;
    }

    /// <summary>
    /// Gets or sets the progress reporter for tracking pipeline execution.
    /// </summary>
    public IProgress<ImportProgress>? Progress { get; set; }

    /// <summary>
    /// Gets or sets the diagnostics collector for emitting telemetry data.
    /// </summary>
    public IDiagnosticsCollector? DiagnosticsCollector { get; set; }

    /// <summary>
    /// Creates the default note detector with standard algorithmic components.
    /// </summary>
    private AlgorithmicNoteDetector CreateDefaultNoteDetector()
    {
        // Create default detector instances with diagnostics
        var onsetDetector = new SpectralFluxOnsetDetector(
            new OnsetDetectionOptions { DiagnosticsCollector = DiagnosticsCollector });

        var pitchDetector = new PyinPitchDetector(
            new PitchDetectionOptions { DiagnosticsCollector = DiagnosticsCollector });

        var timeSignatureDetector = new SimpleTimeSignatureDetector(
            new TimeSignatureDetectionOptions { DiagnosticsCollector = DiagnosticsCollector });

        var tempoDetector = new InterOnsetIntervalTempoDetector(
            new TempoDetectionOptions { DiagnosticsCollector = DiagnosticsCollector });

        var quantizer = new SimpleMonophonicQuantizer(
            new Quantization.QuantizationOptions());

        var boundaryDetector = new EnergyBasedBoundaryDetector(
            new BoundaryDetectionOptions { DiagnosticsCollector = DiagnosticsCollector });

        // Create algorithmic detector with all components
        return new AlgorithmicNoteDetector(
            onsetDetector,
            pitchDetector,
            timeSignatureDetector,
            tempoDetector,
            quantizer,
            boundaryDetector,
            this);
    }

    /// <summary>
    /// Gets the default pipeline options with algorithmic note detection.
    /// </summary>
    public static AudioPipelineOptions Default => new();
}
