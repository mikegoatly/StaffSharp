namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Base class for pipeline stages that provides common functionality like diagnostics emission and progress reporting.
/// </summary>
internal abstract class PipelineStageBase
{
    protected AudioPipelineOptions Options { get; }

    protected PipelineStageBase(AudioPipelineOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Emits diagnostic data for this stage.
    /// </summary>
    /// <typeparam name="T">The type of the diagnostic value.</typeparam>
    /// <param name="key">The diagnostic key.</param>
    /// <param name="value">The diagnostic value.</param>
    protected void EmitDiagnostics<T>(string key, T value)
    {
        Options.DiagnosticsCollector?.Collect(StageName, key, value);
    }

    /// <summary>
    /// Reports progress for this stage.
    /// </summary>
    /// <param name="message">The progress message.</param>
    protected void ReportProgress(string message)
    {
        Options.Progress?.Report(new(StageName, message));
    }

    /// <summary>
    /// Gets the name of this stage for diagnostics and progress reporting.
    /// </summary>
    protected abstract string StageName { get; }
}
