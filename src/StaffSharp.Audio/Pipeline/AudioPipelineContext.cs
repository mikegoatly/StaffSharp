using StaffSharp.Audio.Diagnostics;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Represents progress through the audio pipeline stages.
/// </summary>
/// <param name="StageName">A descriptive name for the current stage.</param>
public record PipelineProgress(string StageName);

/// <summary>
/// Strongly typed context for audio pipeline execution, containing all intermediate results.
/// </summary>
public sealed class AudioPipelineContext
{
    private readonly PipelineContext _baseContext;

    public AudioPipelineContext(
        IDiagnosticsCollector? diagnosticsCollector = null,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _baseContext = new PipelineContext(diagnosticsCollector, cancellationToken);
        Progress = progress;
    }

    /// <summary>
    /// Gets the diagnostics collector for emitting diagnostic data.
    /// </summary>
    public IDiagnosticsCollector? DiagnosticsCollector => _baseContext.DiagnosticsCollector;

    /// <summary>
    /// Gets the cancellation token for the pipeline execution.
    /// </summary>
    public CancellationToken CancellationToken => _baseContext.CancellationToken;

    /// <summary>
    /// Gets the progress reporter for tracking pipeline stages.
    /// </summary>
    public IProgress<PipelineProgress>? Progress { get; }

    /// <summary>
    /// Emits diagnostic data for the specified stage.
    /// </summary>
    public void EmitDiagnostics<T>(string stageName, string key, Func<T> valueFactory)
    {
        _baseContext.EmitDiagnostics(stageName, key, valueFactory);
    }

    /// <summary>
    /// Emits diagnostic data for the specified stage.
    /// </summary>
    public void EmitDiagnostics<T>(string stageName, string key, T value)
    {
        _baseContext.EmitDiagnostics(stageName, key, value);
    }

    // Strongly typed properties for pipeline data flow

    /// <summary>
    /// The loaded audio buffer from the input stream.
    /// </summary>
    public AudioBuffer? Audio { get; set; }

    /// <summary>
    /// The detected audio boundaries (leading/trailing silence).
    /// </summary>
    public AudioBoundaries? Boundaries { get; set; }

    /// <summary>
    /// The detected onset times in seconds.
    /// </summary>
    public ReadOnlyMemory<double>? Onsets { get; set; }

    /// <summary>
    /// The detected MIDI pitch numbers for each onset.
    /// </summary>
    public ReadOnlyMemory<int>? Pitches { get; set; }

    /// <summary>
    /// The detected time signature changes.
    /// </summary>
    public IReadOnlyList<TimeSignatureChange>? TimeSignatures { get; set; }

    /// <summary>
    /// The detected tempo map (tempo changes and time signatures).
    /// </summary>
    public TempoMap? TempoMap { get; set; }

    /// <summary>
    /// The quantized note events (performance timeline data).
    /// </summary>
    public IReadOnlyList<QuantizedNoteEvent>? QuantizedNotes { get; set; }

    /// <summary>
    /// The complete performance timeline (IR1).
    /// </summary>
    public PerformanceTimeline? Timeline { get; set; }
}
