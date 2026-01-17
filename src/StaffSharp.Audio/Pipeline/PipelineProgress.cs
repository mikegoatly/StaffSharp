using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Audio.Pipeline;

public sealed record PipelineProgress
{
    private readonly IProgress<ImportProgress>? _progress;
    private readonly IDiagnosticsCollector? _diagnosticsCollector;

    public static PipelineProgress ForPipeline(AudioPipelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new PipelineProgress(options.Progress, options.DiagnosticsCollector, "Pipeline");
    }

    private PipelineProgress(IProgress<ImportProgress>? progress, IDiagnosticsCollector? diagnosticsCollector, string stageName)
    {
        _progress = progress;
        _diagnosticsCollector = diagnosticsCollector;
        StageName = stageName;
    }

    public static PipelineProgress Null { get; } = new(null, null, "Null");

    public bool DiagnosticsEnabled => _diagnosticsCollector is not null;

    /// <summary>
    /// Emits diagnostic data for this stage.
    /// </summary>
    /// <typeparam name="T">The type of the diagnostic value.</typeparam>
    /// <param name="key">The diagnostic key.</param>
    /// <param name="value">The diagnostic value.</param>
    public void EmitDiagnostics<T>(string key, T value)
    {
        _diagnosticsCollector?.Collect(StageName, key, value);
    }

    /// <summary>
    /// Reports progress for this stage.
    /// </summary>
    /// <param name="message">The progress message.</param>
    public void ReportProgress(string message)
    {
        _progress?.Report(new(StageName, message));
    }

    /// <summary>
    /// Gets the name of this stage for diagnostics and progress reporting.
    /// </summary>
    public string StageName { get; init; }
}
