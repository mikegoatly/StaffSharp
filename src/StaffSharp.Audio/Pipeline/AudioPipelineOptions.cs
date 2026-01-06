using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Analysis.Quantization;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Configuration options for the audio-to-score pipeline.
/// </summary>
public sealed record AudioPipelineOptions
{
    private IAudioBoundaryDetector? _boundaryDetector;
    private IOnsetDetector? _onsetDetector;
    private IPitchDetector? _pitchDetector;
    private ITempoDetector? _tempoDetector;
    private IQuantizer? _quantizer;
    private ITimeSignatureDetector? _timeSignatureDetector;

    public BoundaryDetectionOptions BoundaryDetectionOptions { get; } = new();
    public OnsetDetectionOptions OnsetDetectionOptions { get; } = new();
    public PitchDetectionOptions PitchDetectionOptions { get; } = new();
    public TempoDetectionOptions TempoDetectionOptions { get; } = new();
    public QuantizationOptions QuantizationOptions { get; } = new();
    public TimeSignatureDetectionOptions TimeSignatureDetectionOptions { get; } = new();

    /// <summary>
    /// Gets or sets the boundary detector for identifying leading/trailing silence.
    /// </summary>
    public IAudioBoundaryDetector BoundaryDetector
    {
        get
        {
            // Lazy initialization to inject diagnostics collector
            _boundaryDetector ??= new EnergyBasedBoundaryDetector(BoundaryDetectionOptions with { DiagnosticsCollector = DiagnosticsCollector });
            return _boundaryDetector;
        }

        set => _boundaryDetector = value;
    }

    /// <summary>
    /// Gets or sets the onset detector for identifying note attacks.
    /// </summary>
    public IOnsetDetector OnsetDetector
    {
        get
        {
            // Lazy initialization to inject diagnostics collector
            _onsetDetector ??= new SpectralFluxOnsetDetector(OnsetDetectionOptions with { DiagnosticsCollector = DiagnosticsCollector });
            return _onsetDetector;
        }

        set => _onsetDetector = value;
    }

    /// <summary>
    /// Gets or sets the pitch detector for analyzing fundamental frequency.
    /// </summary>
    public IPitchDetector PitchDetector
    {
        get
        {
            // Lazy initialization to inject diagnostics collector
            _pitchDetector ??= new YinPitchDetector(PitchDetectionOptions with { DiagnosticsCollector = DiagnosticsCollector });
            return _pitchDetector;
        }

        set => _pitchDetector = value;
    }

    /// <summary>
    /// Gets or sets the time signature detector. If null, defaults to 4/4 time.
    /// </summary>
    public ITimeSignatureDetector? TimeSignatureDetector
    {
        get
        {
            _timeSignatureDetector ??= new SimpleTimeSignatureDetector(TimeSignatureDetectionOptions with { DiagnosticsCollector = DiagnosticsCollector });
            return _timeSignatureDetector;
        }

        set => _timeSignatureDetector = value;
    }

    /// <summary>
    /// Gets or sets the tempo detector for analyzing beat timing.
    /// </summary>
    public ITempoDetector TempoDetector
    {
        get
        {
            _tempoDetector ??= new InterOnsetIntervalTempoDetector(TempoDetectionOptions with { DiagnosticsCollector = DiagnosticsCollector });
            return _tempoDetector;
        }

        set => _tempoDetector = value;
    }

    /// <summary>
    /// Gets or sets the quantizer for aligning detected notes to rhythmic grid.
    /// </summary>
    public IQuantizer Quantizer
    {
        get
        {
            _quantizer ??= new SimpleQuantizer(QuantizationOptions with { DiagnosticsCollector = DiagnosticsCollector });
            return _quantizer;
        }

        set => _quantizer = value;
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
    /// Gets the default pipeline options with standard detectors/analyzers.
    /// </summary>
    public static AudioPipelineOptions Default => new();
}
